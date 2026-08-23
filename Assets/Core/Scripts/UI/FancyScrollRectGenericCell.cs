using UnityEngine.UI.Extensions;

namespace Core.UI
{
    public class FancyScrollRectGenericCell : FancyScrollRectCell<FancyScrollRectGenericItemData, FancyScrollRectGenericContext>
    {
        private int m_cellInstanceId;

        public override void Initialize()
        {
            m_cellInstanceId = Context.InitCell(this, transform);
        }

        public override void UpdateContent(FancyScrollRectGenericItemData itemData)
        {
            Context.UpdateCellContent(m_cellInstanceId, Index);
        }
    }
}