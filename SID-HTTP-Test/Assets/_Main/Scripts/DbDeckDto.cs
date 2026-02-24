using System;

[Serializable]
public class DbDeckDto
{
    public int id;
    public string ownerName;
    public DbCardDto[] cards;
}