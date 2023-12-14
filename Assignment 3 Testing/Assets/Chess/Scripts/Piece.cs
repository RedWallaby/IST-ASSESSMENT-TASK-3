using UnityEngine;

[CreateAssetMenu(menuName = "Piece")]
public class Piece : ScriptableObject
{
    public PieceType type;
    public Sprite sprite;
    public Sprite sprite1;
    public bool isWhite;
}

public enum PieceType
{ 
    none = 0,
    pawn = 1 << 0,
    knight = 1 << 1,
    bishop = 1 << 2,
    rook = 1 << 3,
    queen = 1 << 4,
    king = 1 << 5,
    cannon = 1 << 6,
    piercer = 1 << 7,
    juggernaut = 1 << 8,
    necromancer = 1 << 9,
    doubleKnight = 1 << 10,
    //newer pieces
    checker = 1 << 11,
	checkerKing = 1 << 12,
	rammer = 1 << 13,
    lanceKnight = 1 << 14,
    sweeper = 1 << 15,
    alchemist = 1 << 16,
    //custom pieces entirely
    mimic = 1 << 17,
    timeManipulator = 1 << 18,
    stunner = 1 << 19,


    freeMover = 1 << 32,
}
