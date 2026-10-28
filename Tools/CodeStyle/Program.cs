using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(".");
var paths = Directory.GetFiles(Path.Combine(root, "Assets/_LikeShikaku/Scripts"), "*.cs", SearchOption.AllDirectories)
    .Concat(Directory.GetFiles(Path.Combine(root, "Assets/Core/Scripts"), "*.cs", SearchOption.AllDirectories))
    .Concat(Directory.GetFiles(Path.Combine(root, "Tools/GameVerification"), "*.cs", SearchOption.TopDirectoryOnly)).ToArray();
var trees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: new[] { "UNITY_EDITOR", "UNITY_2022_3" }), path)).ToArray();
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).ToList();
references.AddRange(Directory.GetFiles("D:/UnityEditor/2022.3.62f3/Editor/Data/Managed/UnityEngine", "*.dll"));
if (Directory.Exists("D:/UnityEditor/2022.3.62f3/Editor/Data/Managed/UnityEditor"))
    references.AddRange(Directory.GetFiles("D:/UnityEditor/2022.3.62f3/Editor/Data/Managed/UnityEditor", "*.dll"));
references.AddRange(Directory.GetFiles(Path.Combine(root, "Library/ScriptAssemblies"), "*.dll").Where(p => !Path.GetFileName(p).StartsWith("Assembly-CSharp") && !Path.GetFileName(p).StartsWith("KingdomFormation")));
references.Add(Path.Combine(root, "Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll"));
references.AddRange(Directory.GetFiles(Path.Combine(root, "Assets/Plugins/Sirenix/Assemblies"), "*.dll"));
var compilation = CSharpCompilation.Create("StyleAnalysis", trees, references.Where(File.Exists).Distinct().Select(p => MetadataReference.CreateFromFile(p)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
int changed = 0;
foreach (var tree in trees)
{
    if (tree.FilePath.Contains("Assets/Core/") || tree.FilePath.EndsWith("GameType.cs") || tree.FilePath.EndsWith("GameEvents.cs")) continue;
    var model = compilation.GetSemanticModel(tree);
    var renamed = new MemberNames(model).Visit(tree.GetRoot());
    var normalized = new Layout().Visit(renamed).NormalizeWhitespace("    ", "\r\n").ToFullString() + "\r\n";
    if (File.ReadAllText(tree.FilePath) == normalized) continue;
    File.WriteAllText(tree.FilePath, normalized, new System.Text.UTF8Encoding(false));
    changed++;
}
Console.WriteLine("Formatted " + changed + " files: private m_ fields, enum prefixes, braces and member spacing.");

sealed class MemberNames : CSharpSyntaxRewriter
{
    private readonly SemanticModel m_model;
    private static readonly Dictionary<string, string> s_enums = new()
    {
        ["ArmyType"] = "E_ArmyType", ["FormationShape"] = "E_FormationShape", ["RuleFailure"] = "E_ShikakuRuleFailure",
        ["PlayStatus"] = "E_ShikakuPlayStatus", ["TutorialAction"] = "E_TutorialAction"
    };
    public MemberNames(SemanticModel model) { m_model = model; }
    private string Name(ISymbol symbol)
    {
        if (symbol is IFieldSymbol field && field.DeclaredAccessibility == Accessibility.Private && !field.IsConst && !field.IsImplicitlyDeclared)
        {
            if (field.Name.StartsWith("m_") || field.Name.StartsWith("s_")) return field.Name;
            return (field.IsStatic ? "s_" : "m_") + char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1);
        }
        if (symbol is INamedTypeSymbol type && type.TypeKind == TypeKind.Enum && type.ContainingNamespace.ToString() == "MainScene.BattlePart.Shikaku" && s_enums.TryGetValue(type.Name, out string renamed)) return renamed;
        return null;
    }
    public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
    {
        string name = Name(m_model.GetSymbolInfo(node).Symbol);
        return name == null ? base.VisitIdentifierName(node) : node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, name, node.Identifier.TrailingTrivia));
    }
    public override SyntaxNode VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        string name = Name(m_model.GetDeclaredSymbol(node));
        var visited = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node);
        return name == null ? visited : visited.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, name, node.Identifier.TrailingTrivia));
    }
    public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        string name = Name(m_model.GetDeclaredSymbol(node));
        var visited = (EnumDeclarationSyntax)base.VisitEnumDeclaration(node);
        return name == null ? visited : visited.WithIdentifier(SyntaxFactory.Identifier(name));
    }
}

sealed class Layout : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitBlock(BlockSyntax node)
    {
        var visited = (BlockSyntax)base.VisitBlock(node);
        var assignments = new Dictionary<string, string>();
        var statements = new List<StatementSyntax>();
        foreach (var statement in visited.Statements)
        {
            if (statement is ExpressionStatementSyntax expression && expression.Expression is AssignmentExpressionSyntax assignment && assignment.Left is MemberAccessExpressionSyntax property && assignment.Right is LiteralExpressionSyntax)
            {
                string name = property.Name.Identifier.ValueText;
                if (name is "childControlWidth" or "childControlHeight" or "childForceExpandWidth" or "childForceExpandHeight")
                {
                    string key = assignment.Left.ToString();
                    string value = assignment.Right.ToString();
                    if (assignments.TryGetValue(key, out string previous) && previous == value) continue;
                    assignments[key] = value;
                }
            }
            statements.Add(statement);
        }
        return visited.WithStatements(SyntaxFactory.List(statements));
    }
    private static StatementSyntax Block(StatementSyntax node) => node is BlockSyntax ? node : SyntaxFactory.Block(node);
    public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
    {
        var visited = (IfStatementSyntax)base.VisitIfStatement(node);
        return visited.WithStatement(Block(visited.Statement));
    }
    public override SyntaxNode VisitElseClause(ElseClauseSyntax node)
    {
        var visited = (ElseClauseSyntax)base.VisitElseClause(node);
        return visited.Statement is IfStatementSyntax ? visited : visited.WithStatement(Block(visited.Statement));
    }
    public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
    {
        var visited = (ForEachStatementSyntax)base.VisitForEachStatement(node);
        return visited.WithStatement(Block(visited.Statement));
    }
    public override SyntaxNode VisitForStatement(ForStatementSyntax node)
    {
        var visited = (ForStatementSyntax)base.VisitForStatement(node);
        return visited.WithStatement(Block(visited.Statement));
    }
    public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
    {
        var visited = (WhileStatementSyntax)base.VisitWhileStatement(node);
        return visited.WithStatement(Block(visited.Statement));
    }
    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var visited = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
        if (visited.ExpressionBody == null) return visited;
        StatementSyntax statement = visited.ReturnType.ToString() == "void" ? SyntaxFactory.ExpressionStatement(visited.ExpressionBody.Expression) : SyntaxFactory.ReturnStatement(visited.ExpressionBody.Expression);
        return visited.WithBody(SyntaxFactory.Block(statement)).WithExpressionBody(null).WithSemicolonToken(default);
    }
    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        var visited = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
        var members = new List<MemberDeclarationSyntax>();
        foreach (var member in visited.Members)
        {
            if (member is FieldDeclarationSyntax field && field.Declaration.Variables.Count > 1)
            {
                foreach (var variable in field.Declaration.Variables) members.Add(field.WithDeclaration(field.Declaration.WithVariables(SyntaxFactory.SingletonSeparatedList(variable))));
            }
            else members.Add(member);
        }
        return visited.WithMembers(SyntaxFactory.List(members));
    }
}
