using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public static Board instance = null;
    public GameObject tilePool;

    public Row[] rows;
    private Item[] items;

    private List<Tile> selectedTiles = new List<Tile>();
    private List<Tile> checkedTiles = new List<Tile>();
    private List<Tile> popTiles = new List<Tile>();

    private bool canPop = false;
    private float moveDelay = 0.2f;
    private int popCount = 0;
    private int downCount = 0;

    private int width => rows[0].tiles.Length;
    private int height => rows.Length / 2;

    public void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    public async void Start()
    {
        items = Resources.LoadAll<Item>("Prefabs/Items");

        for (int y = 0; y < rows.Length; y++)
        {
            for(int x = 0; x < width; x++)
            {
                rows[y].tiles[x].SetTile(x, y, items[UnityEngine.Random.Range(0, items.Length)]);
            }
        }
        await CheckAllTiles();
    }

    public async void OnClickTile(Tile _tile)
    {
        popCount = 0;

        if (selectedTiles.Count > 1)
            return;

        selectedTiles.Add(_tile);

        if (selectedTiles.Count == 2)
        {
            if (Array.Exists(selectedTiles[0].checkTiles, x => x == _tile))
            {
                await DoSwap(selectedTiles[0], selectedTiles[1]);
                await CheckAllTiles();

                if (canPop != true && popCount == 0)
                {
                    await DoSwap(selectedTiles[1], selectedTiles[0]);
                    canPop = false;
                }
                else
                {
                    await CheckAllTiles();
                }
            }
            selectedTiles.Clear();
        }
    }

    public async Task DoSwap(Tile _tile1, Tile _tile2)
    {
        Sequence sequence = DOTween.Sequence();
        
        Vector3 tile1Pos = _tile1.transform.position;
        Vector3 tile2Pos = _tile2.transform.position;

        sequence.Join(_tile1.transform.DOMove(tile2Pos, moveDelay))
                .Join(_tile2.transform.DOMove(tile1Pos, moveDelay));

        await sequence.Play().AsyncWaitForCompletion();

        _tile1.transform.SetParent(rows[_tile2.y].transform);
        _tile2.transform.SetParent(rows[_tile1.y].transform);

        _tile1.transform.SetSiblingIndex(_tile2.x);
        _tile2.transform.SetSiblingIndex(_tile1.x);

        rows[_tile1.y].tiles[_tile1.x] = _tile2;
        rows[_tile2.y].tiles[_tile2.x] = _tile1;

        int tmpValue = _tile1.x;
        _tile1.x = _tile2.x;
        _tile2.x = tmpValue;

        tmpValue = _tile1.y;
        _tile1.y = _tile2.y;
        _tile2.y = tmpValue;
    }
    public void CheckTile(Tile _tile)
    {
        for (int i = 0; i < _tile.checkTiles.Length; i++)
        {
            if (_tile.checkTiles[i] != null && _tile.checkTiles[i].item == checkedTiles[0].item && !checkedTiles.Contains(_tile.checkTiles[i]) &&
            (_tile.checkTiles[i].x == checkedTiles[0].x || _tile.checkTiles[i].y == checkedTiles[0].y))
            {
                checkedTiles.Add(_tile.checkTiles[i]);
                CheckTile(_tile.checkTiles[i]);
            }
        }
    }
    public async Task CheckHorizon(Tile _tile)
    {
        if (checkedTiles.Contains(_tile))
            return;

        if (checkedTiles.Count == 0)
        {
            checkedTiles.Add(_tile);
        }

        if (_tile.left != null && _tile.left.item == checkedTiles[0].item)
        {
            checkedTiles.Add(_tile.left);
            await CheckHorizon(_tile.left);
        }
        if (_tile.right != null && _tile.right.item == checkedTiles[0].item)
        {
            checkedTiles.Add(_tile.right);
            await CheckHorizon(_tile.right);
        }
        PopCheck();
    }

    public async Task CheckVertical(Tile _tile)
    {
        if (checkedTiles.Contains(_tile))
            return;

        if (checkedTiles.Count == 0)
        {
            checkedTiles.Add(_tile);
        }
        
        if (_tile.top.y >= height && _tile.top != null && _tile.top.item == checkedTiles[0].item)
        {
            checkedTiles.Add(_tile.top);
            await CheckVertical(_tile.top);
        }
        if (_tile.bottom != null && _tile.bottom.item == checkedTiles[0].item)
        {
            checkedTiles.Add(_tile.bottom);
            await CheckVertical(_tile.bottom);
        }
        PopCheck();
    }

    public async Task CheckAllTiles()
    {
        for (int y = rows.Length - 1 ; y >= height; y--)
        {
            for (int x = 0; x < width; x++)
            {
                checkedTiles.Clear();
                await CheckHorizon(rows[y].tiles[x]);
                checkedTiles.Clear();
                await CheckVertical(rows[y].tiles[x]);
            }
        }
        if (canPop)
        {
            await Pop(popTiles);
        }
    }

    public void PopCheck()
    {
        if (checkedTiles.Count > 2)
        {
            canPop = true;
            popTiles.AddRange(checkedTiles);
        }
    }
    public async Task Pop(List<Tile> _tiles)
    {
        popCount++;
        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < _tiles.Count; i++)
        {
            sequence.Join(_tiles[i].image.transform.DOScale(Vector3.zero, moveDelay));
        }
        await sequence.Play().AsyncWaitForCompletion();
        //await RefreshAll(_tiles);
        await CheckAllDownTiles();

        popTiles.Clear();
        canPop = false;

        await CheckAllTiles();
    }
    public async Task RefreshAll(List<Tile> _tiles)
    {
        for (int i = 0; i < _tiles.Count; i++)
        {
            _tiles[i].SetTile(items[UnityEngine.Random.Range(0, items.Length)]);
        }

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < _tiles.Count; i++)
        {
            sequence.Join(_tiles[i].image.transform.DOScale(Vector3.one, moveDelay));
        }
        await sequence.Play().AsyncWaitForCompletion();

    }
    public async Task CheckAllDownTiles()
    {
        Sequence sequence = DOTween.Sequence();
        List<Tile> downTiles = new List<Tile>();
        List<int> downCounts = new List<int>();
        
        for (int x = 0; x < width; x++)
        {
            downCount = popTiles.FindAll(_tile => _tile.x == x).Count;
            downCounts.Add(downCount);

            if (downCount > 0)
            {
                for (int y = 0; y < rows.Length; y++)
                {
                    if(y + downCount < rows.Length)
                    {
                        downTiles.Add(rows[y].tiles[x]);
                        sequence.Join(rows[y].tiles[x].transform.DOMove(rows[y + downCount].tiles[x].transform.position, moveDelay));
                    }
                }
            }
        }
        await sequence.Play().AsyncWaitForCompletion();

        for (int i = 0; i < popTiles.Count; i++)
        {
            rows[popTiles[i].y].tiles[popTiles[i].x] = null;
            popTiles[i].transform.SetParent(tilePool.transform);
        }

        for (int i = 0; i < downTiles.Count; i++)
        {
            downTiles[i].transform.SetParent(rows[downTiles[i].y + downCounts[downTiles[i].x]].transform);
            rows[downTiles[i].y + downCounts[downTiles[i].x]].tiles[downTiles[i].x] = downTiles[i];
            downTiles[i].transform.SetSiblingIndex(downTiles[i].x);
            downTiles[i].y += downCounts[downTiles[i].x];
        }

        for (int x = 0; x < downCounts.Count; x++)
        {
            Debug.Log($"{x} 번쩨 터진 개수 = {downCounts[x]}");

            if (downCounts[x] > 0)
            {
                for (int y = 0; y < downCounts[x]; y++)
                {
                    //Debug.Log($"{y} 번째");
                    //rows[y].tiles[x] = null;
                    //GameObject curGO = tilePool.transform.GetChild(0).gameObject;
                    //curGO.transform.SetParent(rows[y].transform);
                    //curGO.transform.SetSiblingIndex(x);
                    //rows[y].tiles[x] = curGO.GetComponent<Tile>();
                    //curGO.GetComponent<Tile>().y = y;
                   // curGO.GetComponent<Tile>().x = x;
                    //curGO.transform.localScale = Vector3.one;
                }
            }
        }
    }
}