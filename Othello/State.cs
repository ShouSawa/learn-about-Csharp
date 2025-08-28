//20230714,18,髙寺昌吾
using System;
using System.Collections.Generic;
using System.Text;

namespace Reversi
{
    internal class State : Board
    {
        public Place previousMove;          //この盤面状態の直前の着手セルの位置
        public bool previousIsBlack;        //指定の色で直前の着手を区別
        public int eValue;                  //指定の色でこの盤面の評価値
        public State previousState;         //この状態の親ノード
        public List<State> nextStateList;   //この状態から遷移可能な次状態のリスト．State objを要素とするList(動的配列)．

        public State()
        {
            eValue = 0;
            nextStateList = new List<State>();
        }

        //コピーコンストラクタ3
        public State(Board source, Place previousMove, bool previousIsBlack) : base(source)
        {
            this.previousMove = previousMove;
            this.previousIsBlack = previousIsBlack;
            eValue = 0;
            previousState = null;
            nextStateList = new List<State>();
        }

        //コピーコンストラクタ2
        public State(State source, Place move, bool isBlack) :base(source)
        {
            this.set(move.row, move.column, isBlack);

            this.previousState = source;
            this.previousMove = move;
            this.previousIsBlack = isBlack;
            eValue = 0;
            nextStateList = new List<State>();
        }

        //着手可能マスのeValueWhite/Blackの書き戻し
        public void copyEValueOnCanPlace(Board board, bool isBlack)
        {
            for(int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    if (this.canPlace(i, j, isBlack))
                    {
                        board.setEValue(i, j, isBlack, this.getEValue(i,j,isBlack));
                    }
                }
            }
        }

        // nextStateList を評価値(eValue)の降順または昇順でソートし，先頭からnth番目の要素を返す．
        public State sortNextStateList(bool isMax, int nth)
        {
            if (isMax)       // 降順か？
                this.nextStateList.Sort((a, b) => b.eValue - a.eValue); // 評価値(eValue)の降順でソート
            else
                this.nextStateList.Sort((a, b) => a.eValue - b.eValue); // 評価値(eValue)の昇順でソート
            return this.nextStateList[nth];                             // ソートしたnextStateListの先頭からnth番目の要素を返す
        }
    }
}
