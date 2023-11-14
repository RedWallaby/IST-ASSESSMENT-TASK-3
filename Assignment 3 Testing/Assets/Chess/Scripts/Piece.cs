using UnityEngine;

[CreateAssetMenu(menuName = "Assets/Piece")]
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
}
