using System;
using UnityEngine;

[Serializable]
public class CharacterPageResponse
{
    public PageInfo info;
    public Character[] results;
}

[Serializable]
public class PageInfo
{
    public int count;
    public int pages;
    public string next;
    public string prev;
}

[Serializable]
public class Character
{
    public int id;
    public string name;
    public string species;
    public string status;
    public string image;
}