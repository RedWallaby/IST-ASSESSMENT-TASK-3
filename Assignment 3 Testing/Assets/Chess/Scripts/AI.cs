using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ChessBoard;
using Random = UnityEngine.Random;

public class AI : MonoBehaviour
{
    [Header("Main")]
    public Sides playingSides;
    public Difficulty difficulty;
    public float difficultyPercentage;

    [Header("Tweakables")]
    public int depth;
    public bool autoCompleteMove;
    public bool showMove;
    public bool isMaximising;
    
    [Header("Auto AI Features")]
    public bool autoRunning;
    public int runMoves;
    public float autoMoveDelay;
    public float autoMoveTimer;

    [Header("Commands")]
    public bool activateCommand;
    public Action action;

    [Header("Extra-info")]
    [ReadOnly] public double timeTaken;
    [ReadOnly] public int positions;
    [ReadOnly] public double millisecondsPerPosition;
    [ReadOnly] public double positionsPerMillisecond;
    [ReadOnly] public float immediateEvaluation;
    [ReadOnly] public float evaluation;
    [ReadOnly] public float positionalAdvantage;
    [ReadOnly] public int randomMoves;
    [ReadOnly] public List<Move> whiteBackLog;
    [ReadOnly] public List<Move> blackBackLog;

    private int randomMovesCurrently;


    public TMP_Text moveDelay;


    private List<Slot> resetColor = new();

    public enum Action
    {
        None,
        Minimax,
        Eval,
        Test
    }

    [Flags]
    public enum Sides
    {
        None,
        White,
        Black
    }

    public enum Difficulty //dont really need
    {
        Max, //100%
        High, //85%
        Average, //70%
        Low, //50%
        veryLow, //25%
        Min, //0%
    }

    [Serializable]
    public struct Move
    {
        public AISlot start;
        public AISlot end;
    }

    private void Update()
    {
        if (activateCommand)
        {
            activateCommand = false;
            Slot[] array = board.GetComponentsInChildren<Slot>();
            for (int i = 0; i < array.Length; i++)
            {
                AISlot slot = new()
                {
                    x = array[i].x,
                    y = array[i].y,
                    piece = new()
                };
                if (array[i].piece != null)
                {
                    slot.piece.type = array[i].piece.type;
                    slot.piece.isWhite = array[i].piece.isWhite;
                }
                else
                {
                    slot.piece = null;
                }
                currentGame[i] = slot;
            }
            if (action == Action.None)
            {

            }
            if (action == Action.Minimax)
            {
                ResetColors();

                if (depth < 1) depth = 1;
                positions = 0;
                DateTime before = DateTime.Now;
                Move move = MinimaxRoot(depth, isMaximising);
                TimeSpan span = DateTime.Now.Subtract(before);
                timeTaken = math.round(span.TotalMilliseconds) / 1000;

                millisecondsPerPosition = span.TotalMilliseconds / positions;
                positionsPerMillisecond = positions / span.TotalMilliseconds;

                Debug.Log($"AI found a move in {span.TotalMilliseconds} milliseconds or {timeTaken} seconds");
                print($"Best Move: {move.start.piece.type} {move.start.x}, {move.start.y} To {move.end.x}, {move.end.y}");
                print(positions);

                if (isWhiteTurn)
                {
                    whiteBackLog.Add(move);
                    if (whiteBackLog.Count > 5)
                    {
                        whiteBackLog.RemoveAt(0);
                    }
                }
                else
                {
                    blackBackLog.Add(move);
                    if (blackBackLog.Count > 5)
                    {
                        blackBackLog.RemoveAt(0);
                    }
                }

                if (autoCompleteMove)
                {
                    Slot start = GetSlot(move.start.x, move.start.y);
                    Slot end = GetSlot(move.end.x, move.end.y);
                    start.MovePieceToSlot(end);
                    if (showMove)
                    {
                        resetColor.Add(start);
                        resetColor.Add(end);
                        start.GetComponent<Image>().color = Color.magenta;
                        end.GetComponent<Image>().color = Color.magenta;
                    }
                    
                    isWhiteTurn = !isWhiteTurn;
                    isMaximising = !isMaximising;
                }
                immediateEvaluation = EvaluateBoard();
                positionalAdvantage = evaluation - immediateEvaluation;
            }
            else if (action == Action.Eval)
            {
                print(EvaluateBoard());
            }
			else if (action == Action.Test)
			{
                TestSpeeds();
			}
		}
        if (((playingSides.HasFlag(Sides.White) && isWhiteTurn) || (playingSides.HasFlag(Sides.Black) && !isWhiteTurn)) && autoRunning && runMoves != 0 && isRunning == true)
        {
            autoMoveTimer += Time.deltaTime;
            if (autoMoveTimer >= autoMoveDelay && autoMoveDelay >= 0.5f)
            {
                autoMoveTimer = 0;
                runMoves--;
                activateCommand = true;
                action = Action.Minimax;
            }
            UpdateMoveDelay();
        }
        else
        {
            autoMoveTimer = 0;
        }

        if (runMoves < -1)
        {
            runMoves = -1;
        }
    }

    public void TestSpeeds()
    {
		DateTime before = DateTime.Now;
		for (int i = 0; i < 100000; i++) //~1275 milliseconds
        {
			GetAllMoves(true);
        }
		TimeSpan span = DateTime.Now.Subtract(before);
        print("GetAllMoves: " + span.TotalMilliseconds);

		before = DateTime.Now;
		for (int i = 0; i < 100000; i++) //~64 milliseconds
		{
			EvaluateBoard();
		}
		span = DateTime.Now.Subtract(before);
		print("EvalBoard: " + span.TotalMilliseconds);

		AISlot slot = new()
		{
			x = 4,
			y = 4,
			piece = new()
            {
                //type = PieceType.queen
            },
		};
		before = DateTime.Now;
		for (int i = 0; i < 100000; i++) //
		{
            slot.GetValidSquares();
		}
		span = DateTime.Now.Subtract(before);
		print("ValidSquares: " + span.TotalMilliseconds);
	}

    public void ResetColors()
    {
        foreach (Slot slot in resetColor)
        {
            slot.ResetBaseColour();
        }
        resetColor.Clear();
    }

    public bool InBackLog(List<Move> moves, Move move) //needs improving cause like bruh
    {
        foreach(Move listMove in moves)
        {
            if (listMove.end.piece?.type == PieceType.none) listMove.end.piece = null; //for some reason listMove.end.piece un-nulls itself sometimes(?)
            if (MoveIsMove(move, listMove))
            {
                return true;
            }
        }
        return false;
    }

    public bool MoveIsMove(Move move, Move move1)
    {
        AISlot start = move.start;
        AISlot start1 = move1.start;
        AISlot end = move.end;
        AISlot end1 = move1.end;
        return start.x == start1.x && start.y == start1.y && start.piece?.type == start1.piece?.type && start.piece?.isWhite == start1.piece?.isWhite
            && end.x == end1.x && end.y == end1.y && end.piece?.type == end1.piece?.type && end.piece?.isWhite == end1.piece?.isWhite;
    }

    public Move MinimaxRoot(int depth, bool isMaximisingPlayer)
    {
        Dictionary<float, Move> valueMoves = new();
        randomMovesCurrently = 0;
        if (isMaximisingPlayer)
        {
            List<Move> moves = GetAllMoves(isMaximisingPlayer);

            float bestMove = -9999;
            Move bestMoveToPlay = new();

            foreach (Move move in moves)
            {
                if (move.end.piece != null && (move.end.piece.type == PieceType.king || move.end.piece.type == PieceType.necromancer)) return move;
                MoveWithoutVisual(move.start, move.end);
                float value = Minimax(depth - 1, -10000, 10000, !isMaximisingPlayer);
                UndoMove();

                if (InBackLog(whiteBackLog, move))
                {
                    continue; //prevent repeated moves
                }

                /*if (value >= bestMove)
                {
                    if (value == bestMove)
                    {
                        randomMoves++;
                        randomMovesCurrently++;
                        if (Random.Range(0, 2) > 0.5) //picks a random move if they have equal value
                        {
                            continue; 
                        }
                    }
                    randomMoves -= randomMovesCurrently;
                    randomMovesCurrently = 0;
                    bestMove = value;
                    bestMoveToPlay = move;
                }*/
                valueMoves.TryAdd(value, move);
            }

            evaluation = bestMove;

            float[] values = valueMoves.Keys.ToArray();
            //Array.Sort(values); Array.Reverse(values); //could be optimised(?) without this

            evaluation = values.Max();

            float searchValue = math.lerp(values.Min(), evaluation, difficultyPercentage); //ADD FOR NOT IS MAXIMISING
            print(searchValue);
            print(values.Min());
            print(values.Max());
            return valueMoves[values.OrderBy(x => Math.Abs(x - searchValue)).First()];

        }
        else
        {
            List<Move> moves = GetAllMoves(isMaximisingPlayer);

            float bestMove = 9999;
            Move bestMoveToPlay = new();

            foreach (Move move in moves)
            {
                if (move.end.piece != null && (move.end.piece.type == PieceType.king || move.end.piece.type == PieceType.necromancer)) return move;
                
                MoveWithoutVisual(move.start, move.end);
                float value = Minimax(depth - 1, -10000, 10000, !isMaximisingPlayer);
                UndoMove();

                if (InBackLog(blackBackLog, move))
                {
                    continue; //prevent repeated moves
                }

                if (value <= bestMove)
                {
                    if (value == bestMove)
                    {
                        randomMoves++;
                        randomMovesCurrently++;
                        if (Random.Range(0, 1) > 0.5) //picks a random move if they have equal value
                        {
                            continue;
                        }
                    }
                    randomMoves -= randomMovesCurrently;
                    randomMovesCurrently = 0;
                    bestMove = value;
                    bestMoveToPlay = move;
                }
            }
            evaluation = bestMove;
            return bestMoveToPlay;
        }
    }

    public float Minimax(int depth, float alpha, float beta, bool isMaximisingPlayer)
    {
        if (depth == 0)
        {
            return EvaluateBoard();
        }

        List<Move> moves = GetAllMoves(isMaximisingPlayer);

        if (isMaximisingPlayer)
        {
            float bestMove = -9999;
            foreach (Move move in moves)
            {
                positions++;
                MoveWithoutVisual(move.start, move.end);
                bestMove = math.max(bestMove, Minimax(depth - 1, alpha, beta, !isMaximisingPlayer));
                UndoMove();
                alpha = math.max(alpha, bestMove);
                if (beta <= alpha)
                {
                    return bestMove;
                }
            }
            return bestMove;
        }
        else
        {
            float bestMove = 9999;
            foreach (Move move in moves)
            {
                /*if (depth == 1)
                {
                    print("hi");
                    foreach (Move movie in moves)
                    {
                        print(movie.start.x + " " + movie.start.y + " To " + movie.end.x + " " + movie.end.y);
                    }
                    print(GetSlotInGame(move.start.x, move.start.y).piece?.type); print(move.start.piece?.type);
                    print("The culprit is " + move.start.x + " " + move.start.y + " To " + move.end.x + " " + move.end.y);
                }*/
                positions++;
                MoveWithoutVisual(move.start, move.end);
                bestMove = math.min(bestMove, Minimax(depth - 1, alpha, beta, !isMaximisingPlayer));
                /*foreach (AISlot slot in previousSlots.Peek())
                {
                    print(slot.piece?.type + " " + slot.x + " " + slot.y);
                }*/
                UndoMove();
                beta = math.min(beta, bestMove);
                if (beta <= alpha)
                {
                    return bestMove;
                }
            }
            return bestMove;
        }
    }

    public float EvaluateBoard()
    {
        float totalEval = 0;
        foreach (AISlot slot in currentGame)
        {
            totalEval += GetPieceValue(slot);
        }
        return totalEval;
    }

    public float GetPieceValue(AISlot slot)
    {
        if (slot.piece == null) return 0;
        float value = 0;
        int x = slot.piece.isWhite ? slot.x : 9 - slot.x;
        int y = slot.piece.isWhite ? slot.y : 9 - slot.y;
        switch (slot.piece.type) 
        {
            case PieceType.pawn:
                value = 10 + pawnEval[x, y];
                break;
            case PieceType.bishop:
                value = 30 + bishopEval[x, y];
                break;
            case PieceType.piercer:
                value = 30 + bishopEval[x, y]; //TODO figure out if this needs its own eval
                break;
            case PieceType.knight:
                value = 30 + knightEval[x, y];
                break;
            case PieceType.doubleKnight:
                value = 30 + knightEval[x, y]; //TODO create d-knights's own eval (slight tweaks)
                break;
            case PieceType.rook:
                value = 50 + rookEval[x, y];
                break;
            case PieceType.cannon:
                value = 50 + rookEval[x, y]; //TODO create cannon's own eval
                break;
            case PieceType.queen:
                value = 90 + queenEval[x, y];
                break;
            case PieceType.juggernaut:
                value = 90 + queenEval[x, y]; //TODO create juggernaut's own eval
                break;
            case PieceType.king:
                value = 900 + kingEval[x, y];
                break;
            case PieceType.necromancer: 
                value = 900 + kingEval[x, y]; //TODO figure out if this needs its own eval
                break;
        }
        return slot.piece.isWhite ? value : -value;
    }

    readonly float[,] pawnEval = 
    { 
        { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f },
        { 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f },
        { 1.0f, 1.0f, 2.0f, 3.0f, 4.0f, 4.0f, 3.0f, 2.0f, 1.0f, 1.0f },
        { 0.5f, 0.5f, 1.0f, 1.5f, 2.5f, 2.5f, 1.5f, 1.0f, 0.5f, 0.5f },
        { 0.0f, 0.5f, 1.0f, 1.0f, 2.0f, 2.0f, 1.0f, 1.0f, 0.5f, 0.0f },
        { 0.0f, 0.5f, 0.5f, 1.0f, 1.5f, 1.5f, 1.0f, 0.5f, 0.5f, 0.0f },
        { 0.0f, 0.0f, 0.0f, 0.0f, 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f },
        { 0.5f, -0.5f, -1.0f, -1.0f, 0.0f, 0.0f, -1.0f, -1.0f, -0.5f, 0.5f },
        { 0.5f, 1.0f, 1.0f, 1.0f, -2.0f, -2.0f, 1.0f, 1.0f, 1.0f, 0.5f },
        { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }
    };

    readonly float[,] knightEval =
    {
        { -5.0f, -5.0f, -4.0f, -3.0f, -3.0f, -3.0f, -3.0f, -4.0f, -5.0f, -5.0f },
        { -5.0f, -2.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -2.0f, -5.0f },
        { -4.0f, 0.0f, 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.0f, -4.0f },
        { -3.0f, 0.0f, 1.0f, 1.5f, 2.0f, 2.0f, 1.5f, 1.0f, 0.0f, -3.0f },
        { -3.0f, 0.5f, 1.0f, 2.0f, 2.5f, 2.5f, 2.0f, 1.0f, 0.5f, -3.0f },
        { -3.0f, 0.5f, 1.0f, 2.0f, 2.5f, 2.5f, 2.0f, 1.0f, 0.5f, -3.0f },
        { -3.0f, 0.0f, 1.0f, 1.5f, 2.0f, 2.0f, 1.5f, 1.0f, 0.0f, -3.0f },
        { -4.0f, 0.5f, 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.5f, -4.0f },
        { -5.0f, -2.0f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -2.0f, -5.0f },
        { -5.0f, -5.0f, -4.0f, -3.0f, -3.0f, -3.0f, -3.0f, -4.0f, -5.0f, -5.0f },
    };

    readonly float[,] bishopEval =
    {
        { -2.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -2.0f },
        { -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 0.0f, 0.5f, 1.0f, 1.0f, 0.5f, 0.0f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 0.5f, 0.5f, 1.0f, 1.0f, 0.5f, 0.5f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 0.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f, -1.0f },
        { -1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, -1.0f },
        { -1.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.5f, -1.0f },
        { -2.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -2.0f },
    };

    readonly float[,] rookEval =
    {
        { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f },
        { 0.5f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.5f },
        { 0.0f, 0.0f, 0.0f, 0.0f, 0.5f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f },
    };

    readonly float[,] queenEval =
    {
        { -2.0f, -1.5f, -1.0f, -1.0f, -0.5f, -0.5f, -1.0f, -1.0f, -1.5f, -2.0f },
        { -1.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.5f },
        { -1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -1.0f },
        { -0.5f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -0.5f },
        { -0.5f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -0.5f },
        { -1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -1.0f },
        { -1.0f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, -1.0f },
        { -1.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.5f },
        { -2.0f, -1.5f, -1.0f, -1.0f, -0.5f, -0.5f, -1.0f, -1.0f, -1.5f, -2.0f },
    };

    readonly float[,] kingEval =
    {
        { -3.0f, -3.0f, -4.0f, -4.0f, -5.0f, -5.0f, -4.0f, -4.0f, -3.0f, -3.0f },
        { -3.0f, -3.0f, -4.0f, -4.0f, -5.0f, -5.0f, -4.0f, -4.0f, -3.0f, -3.0f },
        { -3.0f, -3.0f, -4.0f, -4.0f, -5.0f, -5.0f, -4.0f, -4.0f, -3.0f, -3.0f },
        { -3.0f, -3.0f, -4.0f, -4.0f, -5.0f, -5.0f, -4.0f, -4.0f, -3.0f, -3.0f },
        { -3.0f, -3.0f, -4.0f, -4.0f, -5.0f, -5.0f, -4.0f, -4.0f, -3.0f, -3.0f },
        { -2.0f, -2.5f, -3.0f, -3.0f, -4.0f, -4.0f, -3.0f, -3.0f, -2.5f, -2.0f },
        { -1.0f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -1.0f },
        { -0.5f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f },
        { 2.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 2.0f, 2.0f },
        { 2.0f, 3.0f, 1.0f, 0.5f, 0.0f, 0.0f, 0.5f, 1.0f, 3.0f, 2.0f },
    };

    public void HandleDropDown(TMP_Dropdown dropdown)
    {
        int index = dropdown.value;
        if (index == 0)
        {
            playingSides = Sides.White;
        }
        else if (index == 1)
        {
            playingSides = Sides.Black;
        }
        else
        {
            playingSides = Sides.White | Sides.Black;
        }
    }
    public void HandleDepth(TMP_InputField inputField)
    {
        if (!int.TryParse(inputField.text, out int value))
        {
            inputField.text = "0";
        }
        else
        {
            depth = value;
        }
    }
    public void HandleMoveNumber(TMP_InputField inputField)
    {
        if (!int.TryParse(inputField.text, out int value))
        {
            inputField.text = "0";
        }
        else
        {
            runMoves = value;
        }
    }
    public void HandleMoveDelay(TMP_InputField inputField)
    {
        if (!float.TryParse(inputField.text, out float value))
        {
            inputField.text = "0";
        }
        else
        {
            autoMoveDelay = value;
            UpdateMoveDelay();
        }
    }

    public void HandleAutoComplete(Toggle toggle)
    {
        autoCompleteMove = toggle.isOn;
    }
    public void HandleShowMove(Toggle toggle)
    {
        showMove = toggle.isOn;
    }
    public void HandleIsMaximising(Toggle toggle)
    {
        isMaximising = toggle.isOn;
    }
    public void HandleAutoPlaying(Toggle toggle)
    {
        autoRunning = toggle.isOn;
    }
    public void UpdateMoveDelay()
    {
        moveDelay.text = "Next Move: \n" + (autoMoveDelay - autoMoveTimer).ToString();
    }
}

[Serializable]
public class AISlot
{
    public int x;
    public int y;
    public AIPiece piece;
    public bool CanTake(AISlot slot) //helper function (checks if a slot could be taken by the piece on this slot)
    {
        return slot != null && (slot.piece == null || piece.isWhite != slot.piece.isWhite);
    }

    public List<AISlot> GetValidSquares() //all piece movement end locations, put into a list
    {
        List<AISlot> result = new();
        if (piece == null)
        {
            return null; //something went wrong (shouldn't be getting called on a slot with nothing)
        }
        else if (piece.type == PieceType.pawn)
        {
            int addY = piece.isWhite ? -1 : 1;
            AISlot slot1 = GetSlotInGame(x, y + addY);
            if (slot1 != null && slot1.piece == null)
            {
                result.Add(slot1);
                AISlot slot2 = GetSlotInGame(x, y + addY * 2);
                if (slot2 != null && slot2.piece == null)
                {
                    result.Add(slot2);
                }
            }
            for (int i = -1; i < 2; i += 2)
            {
                AISlot slot = GetSlotInGame(x + i, y + addY);
                if (slot != null && slot.piece != null && piece.isWhite != slot.piece.isWhite) //must be a piece in that slot
                {
                    result.Add(slot);
                }
            }
        }
        else if (piece.type == PieceType.knight)
        {
            List<AISlot> tempResult = new() //sadly spamming functions is the most effecient (and least messy) way to do this
            {
                GetSlotInGame(x - 2, y - 1), //Left-up
                GetSlotInGame(x - 2, y + 1), //Left-down
                GetSlotInGame(x - 1, y - 2), //Up-left
                GetSlotInGame(x + 1, y - 2), //Up-right
                GetSlotInGame(x + 2, y - 1), //Right-up
                GetSlotInGame(x + 2, y + 1), //Right-down
                GetSlotInGame(x - 1, y + 2), //Down-left
                GetSlotInGame(x + 1, y + 2), //Down-right
            };
            foreach (AISlot slot in tempResult)
            {
                if (CanTake(slot))
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
                    AISlot slot = GetSlotInGame(x + xC, y + yC);
                    if (CanTake(slot))
                    {
                        result.Add(slot);
                    }
                }
            }
        }
        //CUSTOM PIECES
        else if (piece.type == PieceType.cannon || piece.type == PieceType.necromancer)
        {
            List<AISlot> tempResult = new()
            {
                GetSlotInGame(x - 1, y), //left
                GetSlotInGame(x, y - 1), //up
                GetSlotInGame(x + 1, y), //right
                GetSlotInGame(x, y + 1) //down
            };
            foreach (AISlot slot in tempResult)
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
                    AISlot firstSlot = GetSlotInGame(x + xC, y + yC);
                    AISlot secondSlot = GetSlotInGame(x + xC * 2, y + yC * 2);
                    if (CanTake(secondSlot))
                    {
                        result.Add(secondSlot);
                    }
                    if (CanTake(firstSlot))
                    {
                        result.Add(firstSlot);
                    }
                }
            }
        }
        else if (piece.type == PieceType.doubleKnight)
        {
            List<AISlot> tempResult = new()
            {
                GetSlotInGame(x - 3, y - 1), //Left-up
                GetSlotInGame(x - 3, y + 1), //Left-down
                GetSlotInGame(x - 1, y - 3), //Up-left
                GetSlotInGame(x + 1, y - 3), //Up-right
                GetSlotInGame(x + 3, y - 1), //Right-up
                GetSlotInGame(x + 3, y + 1), //Right-down
                GetSlotInGame(x - 1, y + 3), //Down-left
                GetSlotInGame(x + 1, y + 3), //Down-right
            };
            foreach (AISlot slot in tempResult)
            {
                if (CanTake(slot))
                {
                    result.Add(slot);
                }
            }
        }
        return result;
    }

    public List<AISlot> GetActionSquares() //ability movement end locations (red squares)
    {
        List<AISlot> result = new();
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
            List<AISlot> tempResult = new()
            {
                GetSlotInGame(x - 1, y), //left
                GetSlotInGame(x, y - 1), //up
                GetSlotInGame(x + 1, y), //right
                GetSlotInGame(x, y + 1) //down
            };
            foreach (AISlot slot in tempResult)
            {
                if (CanTake(slot) && slot.piece != null)
                {
                    result.Add(slot);
                }
            }
        }
        return result;
    }

    public List<AISlot> GetSlotsInLine(int xC, int yC, bool onlyEnd = false) //helper function
    {
        List<AISlot> result = new();
        AISlot currentslot = this;
        while (currentslot != null)
        {
            currentslot = GetSlotInGame(currentslot.x + xC, currentslot.y + yC);
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
}

[Serializable]
public class AIPiece
{
    public bool isWhite;
    public PieceType type;
}