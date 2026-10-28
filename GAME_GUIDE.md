# 军师八阵图：运行与关卡制作

## 运行

打开 `Assets/BootScene.unity`，点击 Play。首页提供开始/继续布阵、选关和玩法说明。
从 MainScene 直接点击 Play 也会先回到 BootScene，完成原 Core 框架和对象池初始化。

已实现核心矩形规则、教学、撤回/撤销、三级提示、暂停、重试、星级结算、逐关解锁与本地恢复。
所有军阵均为完整矩形：每阵一道将令，面积与数字相同，最后覆盖全部可用格。
所有棋盘格都需要覆盖。操练关仅使用基础规则；协同关还要求每支弓阵与任意步阵或枪阵共边，角接不算，一支护卫可保护多个弓阵。骑兵不能充当护卫。拒马、山石禁区和固定格的兵种、形状、面积限制仍不启用。
数字 1 支持单格军阵。拖出棋盘后松手只取消；多指输入只跟随发起框选的手指。

棋盘底部的操作按钮固定显示；长说明和教学文字可以滚动。
窗口失焦会暂停，返回后点击继续。撤回、取消和教学步骤不扣错，撤销不返还已使用的容错和提示。
局部合法但会造成后续无解的军阵不会被扣错，可以撤回重排。

## 编辑关卡

打开菜单 **Tools → 军师八阵图 → 关卡编辑器**，快捷键 **Ctrl+Shift+L**。
也可以双击任意 LevelConfig 资源直接打开画板。

正式二十关位于：

`Assets/_LikeShikaku/Resources/Shikaku/Levels/`

关卡顺序由同目录上一级的 `LevelDatabase.asset` 管理，列表顺序就是闯关顺序。
前六关为基础操作与矩形推理操练。第 7～14 关依次引入中军、两翼、纵深、环护、交错、雁行、合围、连营八种布局；第 15～20 关使用转向变阵和多弓阵接应。阵名描述布局意图，不要求唯一固定答案；求解器接受所有满足覆盖和护卫的划分。八种布局是游戏化设计，不代表历史阵法复原。

### 制作步骤

1. 新建关卡，保存到 Assets 内。新关卡会使用一个可解模板并生成新的唯一 ID。
2. 设置行列数，点击“应用尺寸”。缩小棋盘会移除范围外的配置，支持 Undo。
3. 将令笔刷：设置数字与兵种，点击或拖动格子放置。
4. 选择笔刷读取将令数据到左侧；擦除笔刷清理格子。绘制支持 Ctrl+Z / Ctrl+Y。
5. 点击“检查配置与解的数量”，区分配置错误、无解、唯一解、多解或超出搜索预算。
6. 勾选“显示参考划分”检查一个可行解，保存资源。
7. 点击“从 BootScene 试玩本关”。这次只加载当前关卡，不消耗或覆盖正式玩家进度。
8. 完成后退出 Play，点击“加入正式关卡列表”。也可在 LevelDatabase Inspector 中调整顺序。

坐标统一为左上角 `(0,0)`，position.x 为行，position.y 为列。
基础检查包括数字之和等于可用格数，但这不代表有解，制作后仍应运行求解检查。
兵种由矩形唯一的将令决定。关卡信息中的“阵法主题”用于说明和结算；“弓阵护卫”开关决定是否启用军阵间协同。自行绘制将令时须重新检查可解性。

### 教学

画板左侧展开“教学步骤”。每步配置文案、动作（部署、撤回、指定违规）、可选目标格及期望错误类型。
教学按操作语义推进，不依赖屏幕坐标。玩家已提前满足后续部署步骤时会跳过已完成内容。
合法完成整个棋盘会直接通关，不会被可选教学阻碍。

## 美术与字体

继续使用原 Block Prefab、MapRoot、Camera、CellLayer 和 Core.PoolManager。
BattleSettings 资源已配置提供的四兵种、阵旗、地形、孔明立绘和校场背景副本。
源素材目录不变；新素材位于 `Assets/_LikeShikaku/Art/KingdomFormation/`。

界面使用工程已有 MiSans 字体；棋盘使用由它生成的动态 TMP 字体，资源位于 `Resources/Shikaku/BoardFont.asset`。
数字和规则标记高于士兵。改变地块图片不会改变规则。
缺失资源时可执行“检查并补齐游戏资源”。该工具只补缺失引用和首次题库，不覆盖已编辑关卡。

## 脚本结构与风格

- MainGameInstance：原启动入口和玩家基础数据；不依赖未完成的 TT SDK。
- MainSceneGameMode：题库、关卡切换和生命周期，仍由原 World.Tick 驱动。
- BattleManager：组织输入、Model、Simulation、表现、存档和通知。
- ShikakuModel：唯一对局状态，包含布局、容错、历史、教学、暂停和胜负。
- ShikakuSimulation：原按下/拖动/松手流程，不承担显示。
- ShikakuRules：实际提交、恢复和求解共用同一套判定。
- ShikakuSolver：候选矩形与精确覆盖搜索，能区分无解和搜索未完成。
- ShikakuController / Cell：沿用对象池格子与世界坐标表现。
- BattleHUD：首页、说明、选关、暂停、战斗操作和结算。
- LevelConfig / LevelDatabase：可编辑关卡和正式顺序。
- Editor：画板、资源补齐、校验和构建菜单。

新增私有字段采用 m_，枚举采用 E_，四空格缩进、独立大括号、中文职责注释，保持与现有工程一致。
根目录 .editorconfig 保留这些约定。没有更换原 Core 框架或修改原组件 GUID。

## 存档

关卡键为 `Shikaku.KingdomFormation.Save.v1`，包含当前布局、历史、教学、提示、容错、最佳星级。
旧独立演示存档只作为首次兼容读取；规则指纹已升级并包含阵法主题与护卫开关，规则变更后不恢复旧局面，保留已有通关与星级记录。
编辑器单关试玩和自动场景检查使用临时内存进度，不写正式存档。
当前是本地存档，不包含广告、付费、云同步或排行榜。

## 验证与构建

菜单“运行场景冒烟检查”会检查画板绘制/撤销/保存，并从 BootScene 进入后逐关完成题库。
需要先退出 Play 并保存场景。结果写到 `Temp/ShikakuValidation.json`，运行截图位于 Temp。
检查完成后会退出 Play 并恢复原场景，不会覆盖正式进度。

离线规则回归：

~~~powershell
dotnet run --project Tools/GameVerification/CoreRegression.csproj --configuration Release
~~~

Unity API 编译检查：

~~~powershell
dotnet build Tools/GameVerification/UnityIntegrationCompile.csproj --configuration Release
dotnet build Tools/GameVerification/UnityIntegrationCompile.csproj --configuration Release -p:PlayerCheck=true
~~~

工具使用本机 .NET 10 和 Unity 2022.3.62f3 程序集，无额外 NuGet 依赖。
切换机器后调整项目中的 UnityProjectRoot / UnityEditorRoot。

实际发布可执行“构建当前平台游玩版”，工具会先构建原框架所需的 Addressables 内容，再构建玩家程序。
目标平台必须在 Unity Hub 安装对应模块。WebGL 构建结果需要通过 HTTP 服务打开。
本轮验证覆盖 Editor 运行和 Player 条件代码编译，不等同于已完成各平台发布包及真机验证。

## 后续扩展

难度优先通过数字因数组合、将令位置和矩形之间的相互牵制设计。护卫关系为整盘协同目标，不预设哪一支护卫保护哪一支弓阵。约束会减少合法解，是否更有趣或更难仍需试玩验证。
协同只在完整解和通关时检查。中间可缺少护卫；全盘覆盖但缺护卫时保持可编辑，不扣错。求解器及三级提示只使用最终满足协同的解。存档可以恢复待调整的完整棋盘。青色连线只显示已经成立的护卫关系。
正式内容制作可以继续增加章节、关卡和独立的美术/音效反馈；大棋盘的提示搜索可改为分帧或后台快照任务。
平台登录、广告和云存档应接在服务层，不应放进 Model 的规则判定中。
