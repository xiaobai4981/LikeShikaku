using System;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Core.UI
{
    public struct FancyScrollRectGenericItemData
    {

    }

    public class FancyScrollRectGenericContext : FancyScrollRectContext
    {
        public Func<FancyScrollRectGenericCell, Transform, int> InitCell;
        public Action<int, int> UpdateCellContent;
    }
}