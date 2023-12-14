using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//[ExecuteInEditMode]
public class Slot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Piece piece;
    public int x;
    public int y;

    public Cursor cursor;

    private Image image;
    private Image pieceImage;
    public SlotState state;

    private static new readonly Color light = Color.white;
    private static readonly Color dark = new(0.75f, 0.75f, 0.75f, 1);

    public void Setup() //replacement for start function since this is an inactive object on start (run in ChessBoard singleton)
    {
        pieceImage = transform.GetChild(0).GetComponent<Image>(); //getComponentInChildren returns the image component of this object sadly

        image = GetComponent<Image>();
        int siblingIndex = transform.GetSiblingIndex();

        if (siblingIndex != 0)
        {
            x = siblingIndex % 10;
            y = (int) math.floor(siblingIndex / 10);
        }
        if (piece != null)
        {
            piece = Instantiate(piece);
        }
        ResetBaseColour();
        UpdateChanges();
    }

    
    
    public void UpdateChanges() //update actual sprite
    {
        if (piece == null)
        {
            pieceImage.sprite = null;
        }
        else
        {
            pieceImage.sprite = piece.isWhite ? piece.sprite : piece.sprite1;
        }
        pieceImage.enabled = pieceImage.sprite != null;
    }

    public void OnPointerClick(PointerEventData eventData) //Manages everything to do with clicking slots
    {
        if (ChessBoard.moveInProgress || !TutorialSlot()) return;
        
        if (!ChessBoard.isRunning)
        {
            if (cursor.currentOption == null && !ChessBoard.isReplacements)
            {
                piece = null;
                UpdateChanges();
                return;
            }
            if (SlotHasKing(this) && !ChessBoard.isReplacements) return;

            if (!ChessBoard.isReplacements)
            {
                piece = Instantiate(cursor.currentOption.piece);
                if (SlotHasKing(this))
                {
                    if (piece.isWhite)
                    {
                        ChessBoard.whiteKingSlot.piece = null;
                        ChessBoard.whiteKingSlot.UpdateChanges();
                        ChessBoard.whiteKingSlot = this;
                    }
                    else
                    {
                        ChessBoard.blackKingSlot.piece = null;
                        ChessBoard.blackKingSlot.UpdateChanges();
                        ChessBoard.blackKingSlot = this;
                    }
                }
            }
            else if (state == SlotState.placeable)
            {
                piece = Instantiate(cursor.currentOption.piece);
                piece.isWhite = y > 4;
            }
            UpdateChanges();
            return;
        }
        if (cursor.isIsolated)
        {
            if (cursor.isolatedSlots.Contains(this))
            {
                cursor.isIsolated = false;
                cursor.isolatedSlots.Clear();
            }
            else
            {
                return;
            }
		}
		if (cursor.selectedSlot == this)
        {
            PutPieceBackDown();
        }
		else if ((cursor.selectedSlot == null || cursor.selectedSlot != this) && piece != null && piece.isWhite == ChessBoard.isWhiteTurn)
        {
            //only have one selected slot
            PutPieceBackDown();
            cursor.SetSlot(this);

            //Gets all possible moves and displays them
            cursor.validSlots = GetValidSquares();
            foreach (Slot slot in cursor.validSlots)
            {
                slot.image.color = Color.green;
                slot.state = SlotState.moveable;
            }

            List<Slot> actionSquares = GetActionSquares();
            cursor.validSlots.AddRange(actionSquares);
            foreach (Slot slot in actionSquares)
            {
                slot.image.color = Color.red;
                slot.state = SlotState.activatable;
            }
        }
        else if (state == SlotState.moveable) //if the square can be moved to, move to it and swap the turn
        {
            cursor.MoveBetweenSlots(cursor.selectedSlot, this);
            SwapPieces(cursor.selectedSlot);
            cursor.selectedSlot.UpdateChanges();
        }
        else if (state == SlotState.activatable) //if the square can be 'activated', do so relative to the piece's ability
        {
            switch (cursor.selectedSlot.piece.type)
            {
                case PieceType.cannon:
                    cursor.MoveBetweenSlots(cursor.selectedSlot, this); //moves a cannonball
                    piece = null; //delete attacked enemy
                    cursor.GetComponent<Image>().sprite = ChessBoard.extraSprites[0];
                    break;
                case PieceType.necromancer:
                    piece.isWhite = !piece.isWhite;
                    GenerateParticles(cursor.particleSystems[2], piece.isWhite ? Color.white : Color.black);
                    UpdateChanges();
                    TakenSlotKingCheck(this);
                    break;
            }
        }

        if (state != SlotState.normal) //if a move or ability occured, reset for next slot click and swap turn
        {
            ChessBoard.isWhiteTurn = !ChessBoard.isWhiteTurn;
            ChessBoard.ai.isMaximising = ChessBoard.isWhiteTurn;
            cursor.selectedSlot = null;
            cursor.ClearPossibleMoves();
        }
    }

    public void SwapPieces(Slot slot) //for moving pieces
    {
        (slot.piece, piece) = (null, slot.piece);
    }

    public void PutPieceBackDown() //remove the selected slot
    {
        cursor.selectedSlot = null;
        cursor.ClearPossibleMoves();
    }

    public bool SlotHasKing(Slot slot) //check for necromancer or king
    {
        return slot.piece != null && (slot.piece.type == PieceType.king || slot.piece.type == PieceType.necromancer);
    }

    public void TakenSlotKingCheck(Slot slot) //check if the game should end
    {
        if (SlotHasKing(slot))
        {
            ChessBoard.menus.SetMenu(3);
            ChessBoard.menus.SetGameOver(slot.piece.isWhite ? "Black" : "White");
            ChessBoard.isRunning = false;
        }
    }

    public void ResetBaseColour() //create the checkered pattern of the chessboard
    {
        state = SlotState.normal;
        if (y % 2 == 0)
        {
            image.color = x % 2 == 0 ? dark : light;
        }
        if (y % 2 == 1)
        {
            image.color = x % 2 == 0 ? light : dark;
        }

        //if its hovered add hover colour
        if (isHovered) 
        {
            AddColourChange();
        }
    }

    public Slot GetSlot(int x, int y) //gets a slot from 2 coordinates
    {
        if (x < 0 || x > 9 || y < 0 || y > 9) return null; //value out of range
        return transform.parent.GetChild(x + 10 * y).GetComponent<Slot>();
    }

    public bool CanMoveTo(Slot slot) //helper function (checks if a slot could be taken by the piece on this slot)
    {
        return slot != null && (slot.piece == null || piece.isWhite != slot.piece.isWhite);
    }

	public bool CanTake(Slot slot) //helper function (checks if there is a takeable piece on the given slot)
	{
		return slot != null && slot.piece != null && piece.isWhite != slot.piece.isWhite;
	}

	public bool IsEmpty(Slot slot) //helper function (checks if the slot has no piece)
	{
		return slot != null && slot.piece == null;
	}

	public void PieceExtraAbility(Slot cursorSlot) //complete the results of a piece's ability
	{
		if (piece.type == PieceType.pawn)
		{
			if (y == 0 || y == 9)
			{
				bool white = piece.isWhite;
				piece = Instantiate(ChessBoard.queens[0]);
				piece.isWhite = white;
			}
		}
		if (piece.type == PieceType.piercer || piece.type == PieceType.checker)
		{
			if (math.abs(x - cursorSlot.x) < 2) return; //did not move 2 spaces
			Slot slot = GetSlot((x + cursorSlot.x) / 2, (y + cursorSlot.y) / 2); //average slot (slot in middle of target and start)
			if (CanMoveTo(slot))
			{
				TakenSlotKingCheck(slot);
				slot.GenerateDeathParticles(slot.piece);
				slot.piece = null;
			}
			slot.ResetBaseColour();
			slot.UpdateChanges();
            if (piece.type == PieceType.checker || piece.type == PieceType.checkerKing) //try to add chain taking
            {
                foreach (Slot slot1 in GetValidSquares())
                {
                    if (math.abs(slot1.x - x) > 1) //distance is greater than 1
                    {
						slot1.image.color = Color.green;
						slot1.state = SlotState.moveable;
                        cursor.validSlots.Add(slot1);
                        cursor.selectedSlot = this;

						ChessBoard.isWhiteTurn = cursor.selectedSlot.piece.isWhite;

                        //isolate moveable slots
                        cursor.isIsolated = true; 
                        cursor.isolatedSlots.Add(slot1);
						return;
                    }
                }
                if (piece.type == PieceType.checker && ((piece.isWhite && y == 0) || (!piece.isWhite && y == 9)))
                {
                    piece.type = PieceType.checkerKing;
                    UpdateChanges();
			    }
			}
		}
		else if (piece.type == PieceType.juggernaut)
		{
			List<Slot> tempResult = new()
			{
				GetSlot(x - 1, y), //left
                GetSlot(x, y - 1), //up
                GetSlot(x + 1, y), //right
                GetSlot(x, y + 1) //down
            };
			foreach (Slot slot in tempResult)
			{
				if (CanMoveTo(slot))
				{
					TakenSlotKingCheck(slot);
					slot.GenerateDeathParticles(slot.piece);
					slot.piece = null;
					slot.ResetBaseColour();
					slot.UpdateChanges();
				}
			}
		}
	}

	public List<Slot> GetValidSquares() //all piece movement end locations, put into a list
    {
        List<Slot> result = new();
        if (piece == null)
        {
            return null; //something went wrong (shouldn't be getting called on a slot with nothing)
        }
        else if (piece.type == PieceType.pawn)
        {
            int addY = piece.isWhite ? -1 : 1;
            Slot slot1 = GetSlot(x, y + addY);
            if (slot1 != null && slot1.piece == null)
            {
                result.Add(slot1);
                Slot slot2 = GetSlot(x, y + addY * 2);
                if (slot2 != null && slot2.piece == null)
                {
                    result.Add(slot2);
                }
            }
            for (int i = -1; i < 2; i += 2)
            {
                Slot slot = GetSlot(x + i, y + addY);
                if (slot != null && slot.piece != null && piece.isWhite != slot.piece.isWhite) //must be a piece in that slot
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.knight)
        {
            List<Slot> tempResult = new() //sadly spamming functions is the most effecient (and least messy) way to do this
            {
                GetSlot(x - 2, y - 1), //Left-up
                GetSlot(x - 2, y + 1), //Left-down
                GetSlot(x - 1, y - 2), //Up-left
                GetSlot(x + 1, y - 2), //Up-right
                GetSlot(x + 2, y - 1), //Right-up
                GetSlot(x + 2, y + 1), //Right-down
                GetSlot(x - 1, y + 2), //Down-left
                GetSlot(x + 1, y + 2), //Down-right
            };
            foreach (Slot slot in tempResult)
            {
                if (CanMoveTo(slot))
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.bishop || piece.type == PieceType.queen) //piecetype queen shortcut
        {
            for (int xC = -1; xC < 2; xC += 2)
            {
                for (int yC = -1; yC < 2; yC += 2)
                {
                    result.AddRange(GetSlotsInLine(xC, yC)); //loops switch diagonals
                }
            }
        }
        if (piece.type == PieceType.rook || piece.type == PieceType.queen) //piecetype queen shortcut (can't be else if)
        {
            result.AddRange(GetSlotsInLine(-1, 0)); //left
            result.AddRange(GetSlotsInLine(0, -1)); //up
            result.AddRange(GetSlotsInLine(1, 0)); //right
            result.AddRange(GetSlotsInLine(0, 1)); //down
        }
        else if (piece.type == PieceType.king || piece.type == PieceType.juggernaut)
        {
            for (int xC = -1; xC < 2; xC++)
            {
                for (int yC = -1; yC < 2; yC++)
                {
                    Slot slot = GetSlot(x + xC, y + yC);
                    if (CanMoveTo(slot))
                    {
                        result.Add(slot);
                    }
                }
            }
        }
        //CUSTOM PIECES
        else if (piece.type == PieceType.cannon || piece.type == PieceType.necromancer)
        {
            List<Slot> tempResult = new()
            {
                GetSlot(x - 1, y), //left
                GetSlot(x, y - 1), //up
                GetSlot(x + 1, y), //right
                GetSlot(x, y + 1) //down
            };
            foreach (Slot slot in tempResult)
            {
                if (slot != null && slot.piece == null)
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.piercer)
        {
            for (int xC = -1; xC < 2; xC += 2)
            {
                for (int yC = -1; yC < 2; yC += 2)
                {
                    Slot firstSlot = GetSlot(x + xC, y + yC);
                    Slot secondSlot = GetSlot(x + xC * 2, y + yC * 2);
                    if (CanMoveTo(secondSlot))
                    {
                        result.Add(secondSlot);
                    }
                    if (CanMoveTo(firstSlot))
                    {
                        result.Add(firstSlot);
                    }
                }
            }
        }
        else if (piece.type == PieceType.doubleKnight)
        {
            List<Slot> tempResult = new()
            {
                GetSlot(x - 3, y - 1), //Left-up
                GetSlot(x - 3, y + 1), //Left-down
                GetSlot(x - 1, y - 3), //Up-left
                GetSlot(x + 1, y - 3), //Up-right
                GetSlot(x + 3, y - 1), //Right-up
                GetSlot(x + 3, y + 1), //Right-down
                GetSlot(x - 1, y + 3), //Down-left
                GetSlot(x + 1, y + 3), //Down-right
            };
            foreach (Slot slot in tempResult)
            {
                if (CanMoveTo(slot))
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.checker)
        {
			for (int xC = -1; xC < 2; xC += 2)
			{
				Slot firstSlot = GetSlot(x + xC, y + (piece.isWhite ? -1 : 1));
				Slot secondSlot = GetSlot(x + xC * 2, y + (2 * (piece.isWhite ? -1 : 1)));
				if (IsEmpty(secondSlot) && CanTake(firstSlot))
				{
					result.Add(secondSlot);
				}
				else if (IsEmpty(firstSlot))
				{
					result.Add(firstSlot);
				}
			}
		}
		else if (piece.type == PieceType.checkerKing)
		{
			for (int xC = -1; xC < 2; xC += 2)
			{
                for (int yC = -1; yC < 2; yC += 2)
                {
                    Slot firstSlot = GetSlot(x + xC, y + yC);
                    Slot secondSlot = GetSlot(x + xC * 2, y + yC);
                    if (IsEmpty(secondSlot) && CanTake(firstSlot))
                    {
                        result.Add(secondSlot);
                    }
                    else if (IsEmpty(firstSlot))
                    {
                        result.Add(firstSlot);
                    }
                }
			}
		}
		return result;
    }

    public List<Slot> GetActionSquares() //ability movement end locations (red squares)
    {
        List<Slot> result = new();
        if (piece == null)
        {
            return null;
        }
        else if (piece.type == PieceType.cannon)
        {
            result.AddRange(GetSlotsInLine(-1, 0, true)); //left
            result.AddRange(GetSlotsInLine(0, -1, true)); //up
            result.AddRange(GetSlotsInLine(1, 0, true)); //right
            result.AddRange(GetSlotsInLine(0, 1, true)); //down
        }
        else if (piece.type == PieceType.necromancer)
        {
            List<Slot> tempResult = new()
            {
                GetSlot(x - 1, y), //left
                GetSlot(x, y - 1), //up
                GetSlot(x + 1, y), //right
                GetSlot(x, y + 1) //down
            };
            foreach (Slot slot in tempResult)
            {
                if (CanMoveTo(slot) && slot.piece != null)
                {
                    result.Add(slot);
                }
            }
        }
        return result;
    }

    public List<Slot> GetSlotsInLine(int xC, int yC, bool onlyEnd = false) //helper function
    {
        List<Slot> result = new();
        Slot currentslot = this;
        while (currentslot != null)
        {
            currentslot = GetSlot(currentslot.x + xC, currentslot.y + yC);
            if (currentslot == null) break;
            if (currentslot.piece == null)
            {
                if (onlyEnd) continue;
                result.Add(currentslot);
            }
            else
            {
                if (currentslot.piece.isWhite != piece.isWhite) result.Add(currentslot); //allow taking of last piece in line if it is takeable
                break;
            }
        }
        return result;
    }

    public void GenerateParticles(ParticleSystem particleSystem, Color color) //particle death/ability generator
    {
        ParticleSystem system = Instantiate(particleSystem);
        ParticleSystem.MainModule ma = system.main;

        system.transform.position = transform.position;
        ma.startColor = color;
        system.Play();
        Destroy(system.gameObject, ma.startLifetime.constant);
    }

    public void GenerateDeathParticles(Piece piece) //automatically generate the correct particles
    {
        if (piece != null)
        {
            ParticleSystem.MainModule ma;
            ParticleSystem system;
            if (piece.type == PieceType.king || piece.type == PieceType.necromancer)
            {
                system = Instantiate(cursor.particleSystems[1]);
            }
            else
            {
                system = Instantiate(cursor.particleSystems[0]);
            }
            ma = system.main;

            system.transform.position = transform.position;
            ma.startColor = piece.isWhite ? Color.white : Color.black;
            system.Play();
            Destroy(system.gameObject, 10);
        }
    }

    public void MovePieceToSlot(Slot endSlot) //code-run moving between slots, instantly chooses a start and end location
    {
        if (piece == null)
        {
            print("You can't move a null piece dipshit"); 
            return;
        }
            cursor.SetSlot(this);
        cursor.MoveBetweenSlots(this, endSlot);
        if (endSlot.piece != null)
        {
            if (piece.type == PieceType.cannon)
            {
                endSlot.piece = null;
                return;
            }
            else if (piece.type == PieceType.necromancer)
            {
                endSlot.piece.isWhite = !endSlot.piece.isWhite;
                return;
            }
        }
        endSlot.piece = piece;
        piece = null;
        
        UpdateChanges();
    }
    
    private bool TutorialSlot() //runs code to check if the slot can be used
    {
        if (!ChessBoard.isTutorial) return true;

        if (ChessBoard.currentTutorialSlot != this) 
        {
            ChessBoard.tutorialManager.text.text = "Wrong Move!";
            ChessBoard.tutorialManager.CancelInvoke(nameof(ChessBoard.tutorialManager.ReSetText));
            ChessBoard.tutorialManager.Invoke(nameof(ChessBoard.tutorialManager.ReSetText), 1);
            return false; 
        }

        if (ChessBoard.tutorialManager.readyForNextMove)
        {
            ChessBoard.tutorialManager.NextStep();
            return true;
        }

        return false;
    }

    //tile hover code
    readonly float colorChange = 0.1f;
    private bool isHovered = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        AddColourChange();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        image.color = new Color(image.color.r + colorChange, image.color.g + colorChange, image.color.b + colorChange);
    }

    private void AddColourChange()
    {
		image.color = new Color(image.color.r - colorChange, image.color.g - colorChange, image.color.b - colorChange);
	}
}
public enum SlotState
{
    normal = 0,
    moveable = 1 << 0,
    activatable = 1 << 1,
    special = 1 << 2,
    placeable = 1 << 3
}