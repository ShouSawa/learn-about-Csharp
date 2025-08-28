//20230714,18,髙寺昌吾
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Reversi
{
    class BasicAI : IAi
    {
        string name = "BasicAI";
        public int depth;               //先読みの深さ
        public bool isInitialized;      //AIplayer初期化の有無
        public List<State> closedList;      //ゲームの棋譜
        public int[,] weightTable;      //各マスの重みを定義

        public BasicAI()
        {
            depth = 1;
            isInitialized = false;
            closedList = new List<State>();
            weightTable = new int[8, 8];
            initWeight(1);
        }
        public string getName()
        {
            return name;
        }
        private void initWeight(int weight)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    weightTable[i, j] = weight;
                }
            }
        }

        public int getWeight(int row, int column)
        {
            return weightTable[row, column];
        }

        public Place nextHand(ref Board board, Place previousMove, bool isBlack)
        {
            initializeEValue(board);
            return simpleBdSearchNextHand(board, previousMove, isBlack);
            //Efunc efunc = pieces;
            //return simpleNextHandPlus(board, previousMove, isBlack, efunc);
            //simpleNextHandPlus(board, previousMove, isBlack);
            //return simpleNextHand(board,previousMove,isBlack);
            //return new Place(2,4);
        }

        public Place simpleNextHand(Board board, Place previousMove, bool isBlack)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.canPlace(i, j, isBlack))
                    {
                        return new Place(i, j);
                    }
                }
            }
            return null;
        }

        private void initializeEValue(Board board)
        {
            if (!(isInitialized))
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        board.setEValue(i, j, true, 0);
                        board.setEValue(i, j, false, 0);
                    }
                }
                isInitialized = true;
            }
        }

        public Place simpleNextHandPlus(Board board, Place previousMove, bool isBlack)
        {
            incEvalueOfAvailableMoves(board, isBlack);
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.canPlace(i, j, isBlack))
                    {
                        return new Place(i, j);
                    }
                }
            }
            return null;
        }

        private void incEvalueOfAvailableMoves(Board board, bool isBlack)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (board.canPlace(i, j, isBlack))
                    {
                        incEvalue(board, i, j, isBlack);
                    }
                }
            }
        }

        private void incEvalue(Board board, int row, int column, bool isBlack)
        {
            board.setEValue(row, column, isBlack, (board.getEValue(row, column, isBlack) + 1));
        }

        public delegate int Efunc(State state, bool isBlack);

        //置いてあるピースの数
        public int pieces(State state, bool isBlack)
        {
            return state.getPieceNum(isBlack);
        }

        public int canMoves(State state, bool isBlack)
        // (盤面)状態の指定の色の着手数(打てる手の数)を返す．
        {
            int canset = 0;
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (state.canPlace(i, j, isBlack))
                    {
                        canset++;
                    }
                }
            }
            return canset;
        }

        Place simpleNextHandPlus(Board board, Place previousMove, bool isBlack, Efunc efunc)
        {
            bool previousIsBlack = !isBlack;
            State state = new State(board, previousMove, previousIsBlack);  // state/3でboardの複製を生成し，
            int evalue = efunc(state, isBlack);                    // efunc/2を呼び出して

            for (int row = 0; row < 8; row++)                           //左上から右に探して，指定の色の着手可能セル全てについて
                for (int column = 0; column < 8; column++)
                    if (state.canPlace(row, column, isBlack))
                        state.setEValue(row, column, isBlack, evalue);// 指定の色の着手可能セル全ての評価値にefunc/2で算出した評価値を代入し

            state.copyEValueOnCanPlace(board, isBlack);          // boardの参照へ(評価値を)コピーしてGUI側で可視化する．
            return simpleNextHand(board, previousMove, isBlack); // 指定の手番(isBlack)が次に置く場所を左上から右に探して，おけるところに置く
        }

        // 7.5.1 白黒それぞれの1手先読み

        // boardの複製(root)を元に，評価関数と評価基準を設定して1手先読みsimplebdSearch/6で手番の着手を決定．
        // 先読み過程としてboardの着手可能セルの評価値を算出・更新．
        // 着手後の次状態を次の手番で1手先読みし，着手可能セルの評価値を算出・更新．
        //rootと次状態を順にclosedListの末尾に追加して着手を返す．

        public Place simpleBdSearchNextHand(Board board, Place previousMove, bool isBlack)
        {
            // 1)まず，boardの参照から探索木のrootノードを生成
            bool previousIsBlack = !isBlack;
            State root = new State(board, previousMove, previousIsBlack);

            this.depth = 1;                 // depthプロパティを1に設定．

            //            Place nextHand = null;          // 戻り値．1手先読みし，手番の評価基準で選んだボード上の着手した位置を表す
            State nextState = null;        // simplebdSearch/6で1手先読みし，手番の評価基準で選んだ着手後の次状態．

            // 2)	手番の評価基準，手番・相手の評価関数でsimplebdSearch/6を実行し，着手と次状態を決定．
            //             bool isMax = false;         // eValueの昇順で
            bool isMax = true;         // eValueの降順で
            int nthPV = 1;              // 第一候補を選ぶ
            //                                        Efunc currentEf = pieces;   // 白：AIplayer 　の評価関数メソッド名
            //                                        Efunc rivalEf   = pieces;   // 黒：人間player の評価関数メソッド名
            Efunc currentEf = canMoves2Pieces;   // 白：AIplayer 　の評価関数メソッド名    
            Efunc rivalEf = canMoves2Pieces;   // 黒：人間player の評価関数メソッド名
            nextState = simplebdSearch(ref root, isBlack, isMax, nthPV, currentEf, rivalEf);

            // 3)	root状態の手番の各着手可能セルの手番色の評価値をboard側の対応セルの手番色の評価値にコピー(副作用)．
            root.copyEValueOnCanPlace(board, isBlack);
            closedList.Add(root);       // rootをclosedListの末尾に追加．

            // 4)	copyEValueOnCanPlace/2で次状態の相手の手番(相手番)の各着手可能セルの相手番色の評価値を，board側の対応セルの相手番色の評価値にコピー(副作用)．
            nextState.copyEValueOnCanPlace(board, previousIsBlack);
            closedList.Add(nextState);       // 次状態をclosedListの末尾に追加．           

            // 5)	次状態のプロパティから手番の着手を返す．
            return nextState.previousMove;  // nextStateの(直前の)着手(セル)の位置．
        }                                   // // 戻り値．1手先読みし，手番の評価基準で選んだボード上の着手した位置を表す



        // root(親)の手番の着手候補をcurrentEfで評価し，次状態を決定，相手番の着手候補をrivalEfで評価する．次状態を返す．
        public State simplebdSearch(ref State root, bool isBlack, bool isMax, int nthPV, Efunc currentEf, Efunc rivalEf)
        {
            int nth = nthPV - 1;        // 配列では nth = 0 が先頭
            State nextState = null;     // 次状態．

            // 1)	root(親)の手番の着手可能セルそれぞれについて，lookAhead/4で次状態を生成，currentEfで評価し，親のnextStateListの末尾に追加
            expandNodeAndEvalThem(ref root, isBlack, currentEf);

            // 2)	sortNextStateList/2(p.10)で親(root)のnextStateListから着手する次状態を決定．
            nextState = root.sortNextStateList(isMax, nth);  // 配列では nth = 0 が先頭

            // 3)	次状態(nextState)に対し，相手の手番(!isBlack)の着手可能セルそれぞれについて
            //     lookAhead/4で次々状態を生成，rivalEfで評価し，次状態のnextStateListの末尾に追加
            expandNodeAndEvalThem(ref nextState, !isBlack, rivalEf); // 次々状態を生成・評価，

            // 4)	次状態を返す． 
            return nextState;
        }



        // 親状態から手番の着手可能セルそれぞれについて，lookAhead/4で次状態を生成・指定された評価関数で評価し、
        // (次状態を)親のnextStateListの末尾に追加する．
        private void expandNodeAndEvalThem(ref State state, bool isBlack, Efunc efunc)
        {
            // 1)	root(親)の手番の着手可能セルそれぞれについて，lookAhead/4 (p.4)で次状態を生成，
            State nextState = null;                         // 次状態．
            for (int row = 0; row < 8; row++)               //左上から右に探して，指定の色の着手可能セル全てについて
                for (int column = 0; column < 8; column++)
                    if (state.canPlace(row, column, isBlack))
                    {
                        Place move = new Place(row, column);
                        nextState = lookAhead(ref state, move, isBlack, efunc); // currentEfで(その次状態を)評価し，
                        state.nextStateList.Add(nextState);                         // (次状態を)親のnextStateListの末尾に追加する．
                    }

            displayCurrentMoves(state, 20);//手数表示（場所適当）

        }


        // moveに石を置いて次状態を生成、次状態の評価値をefuncで算出、評価値をboardに代入。次状態を返す

        private State lookAhead(ref State state, Place move, bool isBlack, Efunc efunc)
        {
            State nextState = new State(state, move, isBlack);  // moveに着手(isBlack色として）
            int evalue = efunc(nextState, isBlack);  // 評価関数efuncを呼んで評価値evalue に代入（isBlack色の）
            nextState.eValue = evalue;  // 状態の評価値
            state.setEValue(move.row, move.column, isBlack, evalue);  // stateで(row, column)にisBlackを置くと評価値evalueを持つ状態になる
            return nextState;  // 次状態を返す

        }

        public int weightedPieces(State state, bool isBlack)
        //指定の色の石のある升目のweightTableの重みの和を返す
        {
            int SUM = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    SUM += state.getEValue(i, j, isBlack);
                }
            }
            return SUM;

        }

        public int countMoves(State state)
        {
            int SUM = -4;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (!(state.isBlank(i, j)))
                    {
                        SUM++;
                    }
                }
            }
            return SUM;
        }

        public int canMoves2Pieces(State state, bool isBlack)
        {
            if (countMoves(state) <= 40)
            {
                return canMoves(state, isBlack);
            }
            else
            {
                return pieces(state, isBlack);
            }
        }

        public void displayCurrentMoves(State state, int interval)
        {
            if (countMoves(state) % interval == 0)
            {
                Debug.WriteLine("現在の手数：" + countMoves(state) + "手");
                Debug.WriteLine("黒：" + pieces(state,true) + "個");
                //Debug.WriteLine("白：" + pieces(state,false) + "個");
            }

        }

        public int imorivedWeithtedPieces(State state, bool isBlack)
        {
            return canMoves2Pieces(state, isBlack) + weightedPieces(state, isBlack);

        }

    }
}