using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum PieceType
{
    None = 0,
    Man = 1,
    King = 2
}

public class Piece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public PieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public List<Vector2Int> GetAvailableMoves(ref Piece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        //r.Add(new Vector2Int(3, 3));
        //r.Add(new Vector2Int(3, 4));
        //r.Add(new Vector2Int(4, 3));
        //r.Add(new Vector2Int(4, 4));

        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
}
