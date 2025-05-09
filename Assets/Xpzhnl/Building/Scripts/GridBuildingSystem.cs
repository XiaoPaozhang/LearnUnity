using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

//写一个压缩tilmap的边界编辑器菜单
// class TilemapCompress : Editor
// {
//   [MenuItem("Tools/Tilemap/Compress Bounds")]
//   static void CompressBounds()
//   {
//     Tilemap tilemap = Selection.activeGameObject.GetComponent<Tilemap>();
//     BoundsInt bounds = tilemap.cellBounds;
//     tilemap.CompressBounds();
//     Debug.Log($"压缩前：{bounds} 压缩后：{tilemap.cellBounds}");
//   }
// }
public class GridBuildingSystem : MonoBehaviour
{
  // 单例模式实现
  public static GridBuildingSystem current;
  private void Awake()
  {
    current = this; // 确保全局唯一实例
  }

  // 网格布局组件（用于坐标转换）
  public GridLayout gridLayout;

  // 主瓦片地图（已放置建筑）
  public Tilemap MainTilemap;

  // 临时瓦片地图（预览位置）
  public Tilemap TempTilemap;

  // Tile类型字典（空/绿/红/可建造）
  private static Dictionary<TileType, TileBase> tileBases = new Dictionary<TileType, TileBase>();

  // 添加这两个私有字段来存储地图数据
  private HashSet<Vector3Int> buildableCells = new HashSet<Vector3Int>(); // 可建造的格子坐标
  private HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();  // 已被占据的格子坐标


  // 当前操作的建筑对象
  private Building temp;

  // 记录前一个位置和区域
  private Vector3 prevPos;
  private BoundsInt prevArea;

  #region Unity生命周期
  void Start()
  {
    string tilePath = @"Tiles/";
    // 加载绿/红指示Tile
    tileBases.Add(TileType.Empty, null);
    tileBases.Add(TileType.Green, Resources.Load<TileBase>(tilePath + "green"));
    tileBases.Add(TileType.Red, Resources.Load<TileBase>(tilePath + "red"));
    tileBases.Add(TileType.White, Resources.Load<TileBase>(tilePath + "GrassRuleTile"));

    // 新增：初始化时扫描MainTilemap获取可建造区域
    InitializeBuildableCells();
  }

  // 初始化可建造区域
  // 修改后的InitializeBuildableCells方法
  private void InitializeBuildableCells()
  {
    BoundsInt bounds = MainTilemap.cellBounds;
    Debug.Log($"扫描范围：{bounds}");

    int validCount = 0;
    foreach (var position in bounds.allPositionsWithin)
    {
      Vector3Int pos = new Vector3Int(position.x, position.y, 0);

      //在位置上找不到瓦片
      if (!MainTilemap.HasTile(pos))
      {
        continue;
      }

      TileBase tile = MainTilemap.GetTile(pos);
      Debug.Log($"发现Tile：位置{pos} 名称{(tile ? tile.name : "null")}");
      validCount++;

      // 检查是否为可放置地块
      bool isWhiteTile = tile.name == tileBases[TileType.White].name;

      if (isWhiteTile)
      {
        buildableCells.Add(pos);
        Debug.Log($"添加可建造格子：{pos}");
      }
    }

    Debug.Log($"扫描完成，有效Tile数量：{validCount}，可建造格子数：{buildableCells.Count}");

  }
  void Update()
  {

    // 在Update中添加临时调试
    if (Input.GetKeyDown(KeyCode.D))
    {
      Debug.Log($"=== 当前可建造坐标,共{buildableCells.Count} ===");
      foreach (var pos in buildableCells)
      {
        Debug.Log(pos);
      }
      Debug.Log($"=== 当前占用坐标,共{occupiedCells.Count} ===");
      foreach (var pos in occupiedCells)
      {
        Debug.Log(pos);
      }
    }

    if (!temp) return; // 没有待放置建筑时退出

    // 当鼠标在UI上时忽略操作
    if (EventSystem.current.IsPointerOverGameObject()) return;

    if (Input.GetMouseButtonDown(0) && temp.CanBePlaced()) // 左键放置
    {
      temp.Place(); // 确认放置
      temp = null;
      return;
    }
    else if (Input.GetMouseButtonDown(1)) // 右键取消
    {
      ClearArea();    // 清除预览
      Destroy(temp.gameObject); // 销毁临时对象
      return;
    }

    // 鼠标移动时的处理
    if (!temp.Placed)
    {
      // 转换鼠标位置到网格坐标
      Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
      Vector3Int cellPos = gridLayout.LocalToCell(touchPos);

      // 当位置变化时更新建筑位置
      if (prevPos != cellPos)
      {
        // 设置建筑到网格中心
        temp.transform.localPosition = gridLayout.CellToLocalInterpolated(cellPos
        + new Vector3(0.5f, 0.5f, 0f)
        );
        prevPos = cellPos;
        FollowBuilding(); // 更新预览
      }
    }

  }
  #endregion
  #region Tilemap Management

  private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap) // 获取区域瓦片返回数组，为什么不用unity API因为会报错
  {
    TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z]; // 计算所有瓦片数量并分配空间
    int counter = 0;
    foreach (var v in area.allPositionsWithin) // 遍历所有瓦片位置
    {
      Vector3Int pos = new Vector3Int(v.x, v.y, 0);
      array[counter] = tilemap.GetTile(pos);
      counter++;
    }

    return array;
  }

  /// <summary>
  /// 设置瓦片区域  
  /// </summary>
  /// <param name="area">区域</param>
  /// <param name="type">瓦片类型</param>
  /// <param name="tilemap">瓦片地图</param>
  private static void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap)
  {
    int size = area.size.x * area.size.y * area.size.z;
    TileBase[] tileArray = new TileBase[size];
    FillTiles(tileArray, type);
    tilemap.SetTilesBlock(area, tileArray); // 设置区域瓦片
  }

  private static void FillTiles(TileBase[] arr, TileType type) // 填充数组，传入我们的枚举即可
  {
    for (int i = 0; i < arr.Length; i++)
    {
      arr[i] = tileBases[type];
    }
  }
  #endregion

  #region Building Placement

  public void InitializeWithBuilding(GameObject building)
  {
    Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    Vector3Int cellPos = gridLayout.LocalToCell(mousePos);
    Vector3 spawnPos = gridLayout.CellToLocalInterpolated(cellPos + new Vector3(0.5f, 0.5f, 0f));

    temp = Instantiate(building, spawnPos, quaternion.identity).GetComponent<Building>();

    // 设置建筑半透明和渲染级别
    SpriteRenderer renderer = temp.transform.Find("Sprite").GetComponent<SpriteRenderer>();
    if (renderer)
    {
      Color semiTransparent = renderer.color;
      semiTransparent.a = 0.7f;
      renderer.color = semiTransparent;
      renderer.sortingOrder = 1;
    }

    FollowBuilding();
  }
  private void ClearArea() // 清除区域
  {
    TileBase[] toClear = new TileBase[prevArea.size.x * prevArea.size.y * prevArea.size.z];
    FillTiles(toClear, TileType.Empty);
    TempTilemap.SetTilesBlock(prevArea, toClear);
  }

  private void FollowBuilding()
  {
    ClearArea();

    temp.area.position = gridLayout.WorldToCell(temp.gameObject.transform.position);
    BoundsInt buildingArea = temp.area;

    TileBase[] tileArray = new TileBase[buildingArea.size.x * buildingArea.size.y * buildingArea.size.z];

    int index = 0;
    foreach (var pos in buildingArea.allPositionsWithin)
    {
      Vector3Int gridPos = new Vector3Int(pos.x, pos.y, 0);

      // 新的数据驱动判断逻辑
      bool isBuildable = buildableCells.Contains(gridPos);
      bool isOccupied = occupiedCells.Contains(gridPos);

      if (isBuildable && !isOccupied)
      {
        tileArray[index] = tileBases[TileType.Green];
      }
      else
      {
        tileArray[index] = tileBases[TileType.Red];
      }
      index++;
    }

    TempTilemap.SetTilesBlock(buildingArea, tileArray);
    prevArea = buildingArea;
  }

  /// <summary>
  /// 是否可以建造
  /// </summary>
  /// <param name="area"></param>
  /// <returns></returns>
  public bool CanTakeArea(BoundsInt area)
  {
    foreach (var position in area.allPositionsWithin)
    {
      Vector3Int gridPos = new Vector3Int(position.x, position.y, 0);

      // 双重验证：既是可建造区域 && 未被占用
      if (!buildableCells.Contains(gridPos) || occupiedCells.Contains(gridPos))
      {
        Debug.Log($"禁止放置：位置{gridPos} " +
                 $"可建造：{buildableCells.Contains(gridPos)} " +
                 $"已占用：{occupiedCells.Contains(gridPos)}");
        return false;
      }
    }
    return true;
  }

  /// <summary>
  /// 放置建筑区域
  /// </summary>
  /// <param name="area"></param>
  public void TakeArea(BoundsInt area)
  {
    // 清除临时Tilemap的显示
    SetTilesBlock(area, TileType.Empty, TempTilemap);

    // 更新数据存储
    foreach (var pos in area.allPositionsWithin)
    {
      Vector3Int gridPos = new Vector3Int(pos.x, pos.y, 0);
      occupiedCells.Add(gridPos);
      buildableCells.Remove(gridPos);
    }
  }

  #endregion
  // 修改 GridBuildingSystem.cs 的OnDrawGizmos
  void OnDrawGizmos()
  {
    if (!Application.isPlaying) return;

    Gizmos.color = Color.green;
    foreach (var pos in buildableCells)
    {
      // 使用Tilemap的坐标系转换
      Vector3 center = MainTilemap.CellToWorld(pos) + MainTilemap.cellSize / 2;
      Gizmos.DrawWireCube(center, MainTilemap.cellSize * 0.9f);
    }
  }
}

public enum TileType
{
  Empty,
  White, // 保留枚举值但不再直接使用对应的Tile
  Green,
  Red,
}
