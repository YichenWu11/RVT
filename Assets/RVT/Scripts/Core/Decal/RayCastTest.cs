using UnityEngine;

public class RayCastTest : MonoBehaviour
{
    public Rect regionRect;
    public Camera mainCamera;
    public int tableSize = 64;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var hitTileCoord = CalculateTileCoord(new Vector2(hit.point.x, hit.point.z));
                Debug.Log($"hit {hit.collider.name} at ({hitTileCoord.x},{hitTileCoord.y})");
            }
        }
    }

    private Vector2Int CalculateTileCoord(Vector2 hitPos)
    {
        var uv = hitPos / regionRect.size;
        return new Vector2Int(
            Mathf.FloorToInt(uv.x * tableSize),
            Mathf.FloorToInt(uv.y * tableSize));
    }
}