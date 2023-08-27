using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(GridLayoutGroup))]
public class AdjustGridLayoutCellSize : MonoBehaviour
{
    [SerializeField]
    public Vector2 cellSize = new(100, 100);

    public enum Axis { X, Y };
    public enum RatioMode { Free, Fixed };

    [SerializeField] Axis expand;
    [SerializeField] RatioMode ratioMode;
    //[SerializeField] float cellRatio = 1;

    RectTransform rectTranform;
    GridLayoutGroup grid;

    void Awake()
    {
        rectTranform = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateCellSize();
    }

    void OnRectTransformDimensionsChange()
    {
        UpdateCellSize();
    }

#if UNITY_EDITOR
    [ExecuteAlways]
    void Update()
    {
        UpdateCellSize();
    }
#endif

    void OnValidate()
    {
        rectTranform = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();
        UpdateCellSize();
    }

    void UpdateCellSize()
    {
        if (grid == null) return;
        //var count = grid.constraintCount;
        if (expand == Axis.X)
        {
            if (grid.cellSize.x == 0) return;
            float count = Mathf.Floor((rectTranform.rect.width - grid.padding.left - grid.padding.right) / grid.cellSize.x);
            float height = Mathf.CeilToInt(transform.childCount / count) * grid.cellSize.y;
            height = (height != 0) ? height : grid.cellSize.y;
            rectTranform.sizeDelta = new Vector2(0, height + grid.padding.top + grid.padding.bottom);
        }
        else //if (expand == Axis.Y)
        {
            //float spacing = (count - 1) * grid.spacing.y;
            //float contentSize = transform.rect.height - grid.padding.top - grid.padding.bottom - spacing;
            //float sizePerCell = contentSize / count;
            //grid.cellSize = new Vector2(ratioMode == RatioMode.Free ? grid.cellSize.x : sizePerCell * cellRatio, sizePerCell);
        }
    }
}