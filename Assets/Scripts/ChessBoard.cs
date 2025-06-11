using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private float tileSize = 0.6f;
    [SerializeField] private float yOffset = 0.363f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private GameObject manPrefab;
    [SerializeField] private GameObject kingPrefab;
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.415f;
    [SerializeField] private float dragOffset = 0.5f;

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private Piece[,] pieces;
    private Piece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<Piece> deadWhites = new List<Piece>();
    private List<Piece> deadBlacks = new List<Piece>();
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private Color[,] originalColors;

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            Vector2Int hitPosition = LookupTileIndex(hit.transform.gameObject);

            if (currentHover != hitPosition)
            {
                if (currentHover != -Vector2Int.one)
                {
                    var prevRenderer = tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>();
                    prevRenderer.material.color = originalColors[currentHover.x, currentHover.y];
                }

                var renderer = tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>();
                renderer.material.color = hoverMaterial.color;

                currentHover = hitPosition;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (pieces[hitPosition.x, hitPosition.y] != null)
                {
                    if (true)
                    {
                        currentlyDragging = pieces[hitPosition.x, hitPosition.y];

                        availableMoves = currentlyDragging.GetAvailableMoves(ref pieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();

                    }
                }
            }

            if (currentlyDragging != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                var renderer = tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>();
                renderer.material.color = originalColors[currentHover.x, currentHover.y];
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        
        availableMoves.Clear();
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        originalColors = new Color[tileCountX, tileCountY];
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }

    private void SpawnAllPieces()
    {
        pieces = new Piece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;
        for (int i = 0; i < TILE_COUNT_X; i += 2)
        {
            pieces[i, 0] = SpawnSinglePiece(PieceType.Man, whiteTeam);
            pieces[i, 2] = SpawnSinglePiece(PieceType.Man, whiteTeam);
            pieces[i + 1, 1] = SpawnSinglePiece(PieceType.Man, whiteTeam);
            pieces[i + 1, 7] = SpawnSinglePiece(PieceType.Man, blackTeam);
            pieces[i + 1, 5] = SpawnSinglePiece(PieceType.Man, blackTeam);
            pieces[i, 6] = SpawnSinglePiece(PieceType.Man, blackTeam);
        }
    }

    private Piece SpawnSinglePiece(PieceType type, int team)
    {
        Piece piece = Instantiate(manPrefab, transform, false).GetComponent<Piece>();
        piece.type = type;
        piece.team = team;
        piece.GetComponentInChildren<MeshRenderer>().material = teamMaterials[team];

        return piece;
    }

    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (pieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        pieces[x, y].currentX = x;
        pieces[x, y].currentY = y;
        pieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format($"X:{x}, Y:{y}"));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        tileObject.GetComponent<MeshRenderer>().material = new Material(tileMaterial); // делаем копию
        originalColors[x, y] = tileObject.GetComponent<MeshRenderer>().material.color;


        return tileObject;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one;
    }

    private bool MoveTo(Piece piece, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(piece.currentX, piece.currentY);

        if (pieces[x, y] != null)
        {
            Piece ocp = pieces[x, y];

            if(piece.team == ocp.team)
            {
                return false;
            }

            if(ocp.team == 0)
            {
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds +
                    new Vector3(tileSize / 2 - 0.2f, 0, tileSize / 2) +
                    (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds +
                    new Vector3(tileSize / 2 + 0.2f, 0, tileSize / 2) +
                    (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
            
        }

        pieces[x, y] = piece;
        pieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        return true;
    }
}
