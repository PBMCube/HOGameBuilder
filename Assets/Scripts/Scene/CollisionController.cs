using System;
using UnityEngine;

// collisions on 2D plane
public class CollisionController : MonoBehaviour
{

    public bool Enabled = true;
    public Vector2Int MatrixSize = new Vector2Int(64, 64);

    private Vector2 sceneSize;
    private bool[,] matrix;

    public CollisionController()
    {
        sceneSize = new Vector2Int(1366, 768);
        matrix = new bool[MatrixSize.y, MatrixSize.x];
    }

    public CollisionController(Vector2Int parentSize)
    {
        sceneSize = parentSize;
        matrix = new bool[MatrixSize.y, MatrixSize.x];
    }

    public bool CheckCollision(ImageDescriptor image)
    {
        return !TryCapturePlace(image.position, image.size);
    }

    public bool CheckCollision(SpriteRenderer image)
    {
        return !TryCapturePlace(image.transform.position, image.sprite.bounds.size);
    }

    private bool TryCapturePlace(Vector3 position, Vector2 size)
    {
        Vector2 scenePoint = new Vector2(position.x + sceneSize.x / 2, position.y + sceneSize.y / 2);
        Vector2Int cellDown = new Vector2Int((int)System.Math.Floor((scenePoint.x - size.x / 2) * MatrixSize.x / sceneSize.x),
                                             (int)System.Math.Floor((scenePoint.y - size.y / 2) * MatrixSize.y / sceneSize.y));
        Vector2Int cellUp = new Vector2Int((int)System.Math.Ceiling((scenePoint.x + size.x / 2) * MatrixSize.x / sceneSize.x),
                                           (int)System.Math.Ceiling((scenePoint.y + size.y / 2) * MatrixSize.y / sceneSize.y));

        cellDown = new Vector2Int(Math.Max(cellDown.x, 0), Math.Max(cellDown.y, 0));
        cellUp   = new Vector2Int(Math.Min(cellUp.x, MatrixSize.x), Math.Min(cellUp.y, MatrixSize.y));

        for (int i = cellDown.y; i < cellUp.y; i++)
            for (int j = cellDown.x; j < cellUp.x; j++)
            {
                if (matrix[i, j])
                    return false;
            }
        for (int i = cellDown.y; i < cellUp.y; i++)
            for (int j = cellDown.x; j < cellUp.x; j++)
            {
                matrix[i, j] = true;
            }
        return true;
    }    
}
