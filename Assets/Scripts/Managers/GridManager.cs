using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int width, height;
    public GameObject tilePrefab;
    public GameObject playerPrefab;
    public GameObject holePrefab;
    public GameObject wallPrefab;
    public GameObject goalPrefab;
    //public Rock rockPrefab;
    public Transform gridParent;

    public Vector2Int goalPos;

    public Vector3 origin = Vector3.zero;
    private GridCellType[,] gridLogic;
    public List<Vector2Int> holesList;

    private void Awake()
    {
        
    }
}
