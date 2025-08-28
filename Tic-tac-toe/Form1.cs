using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace マルバツゲーム
{
    public partial class Form1 : Form
    {

        Button[] buttons = new Button[12];

        //画面を読み込んだとき
        private void Form1_Load(object sender, EventArgs e)
        {
            buttons[0] = button1;
            buttons[1] = button2;
            buttons[2] = button3;
            buttons[3] = button4;
            buttons[4] = button5;
            buttons[5] = button6;
            buttons[6] = button7;
            buttons[7] = button8;
            buttons[8] = button9;
            buttons[9] = button10;
            buttons[10] = button11;
            buttons[11] = button12;

            label1.Text = "first move or second move?";
            Console.WriteLine("試合開始");
            disabled();
            buttons[9].Enabled = false;
            buttons[10].Enabled = true;
            buttons[11].Enabled = true;
        }

        public Form1()
        {
            InitializeComponent();
        }

        //ボタンを押したとき
        private void button_Click(object sender, EventArgs e)
        {
            bool finish = false;//試合が終わったかどうか

            Button b = (Button)sender;
            string name = b.Name;
            b.Text = "〇";
            b.Enabled = false;
            Console.WriteLine("プレイヤーの一手");

            finish = result();

            Random r = new System.Random();
            int memo;
            if(!(finish))//試合がまだ終わってないとき
            {
                System.Threading.Thread.Sleep(500);
                while (true)
                {
                    int r1 = r.Next(1, 9);

                    if (buttons[r1].Text != "〇" && buttons[r1].Text != "×")
                    {
                        buttons[r1].Text = "×";
                        memo = r1;
                        buttons[memo].Enabled = false;
                        Console.WriteLine("コンピュータの一手");
                        break;
                    }
                }
            }
            finish = result();
        }
        //3*3のボタンを押せなくする
        void disabled()
        {
            for(int k = 0; k < 9; k++)
            {
                buttons[k].Enabled = false;
            }
        }

        //勝敗判定
        private int judge()
        {
            for (int i = 0; i < 3; i++)
           {
               //横の判定
               if (buttons[0 + i * 3].Text == buttons[1 + i * 3].Text && buttons[1 + i * 3].Text == buttons[2 + i * 3].Text)
               {
                   if (buttons[0 + i * 3].Text == "〇")//プレイヤーの勝ち
                    {
                        return 1;
                    }
                   else if (buttons[0 + i * 3].Text == "×")//プレイヤーの負け
                    {
                        return -1;
                    }
               //縦の判定
               }else if (buttons[0 + i].Text == buttons[3 + i].Text && buttons[3 + i].Text == buttons[6 + i].Text)
                    {
                        if (buttons[0 + i].Text == "〇")//プレイヤーの勝ち
                    {
                        return 1;
                        
                    }
                        else if (buttons[0 + i].Text == "×")//プレイヤーの負け
                    {
                        return -1;
                    }
                    }
           }

           //ななめの判定
           if (buttons[0].Text == buttons[4].Text && buttons[4].Text == buttons[8].Text)
           {
               if (buttons[0].Text == "〇")//プレイヤーの勝ち
                {
                    return 1;
                }
               else if (buttons[0].Text == "×")//プレイヤーの負け
                {
                    return -1;
                }
           }else if (buttons[2].Text == buttons[4].Text && buttons[4].Text == buttons[6].Text)
           {
               if (buttons[2].Text == "〇")//プレイヤーの勝ち
                {
                    return 1;
                }
               else if (buttons[2].Text == "×")//プレイヤーの負け
                {
                    return -1;
                }
           }

            //引き分けの判定
            int count = 0;
            for (int j = 0; j < 9; j++)
            {
                if (buttons[j].Text == "〇" || buttons[j].Text == "×")
                {
                    count++;
                }
            }
            if (count == 9)
            {
                return 0;
            }

           return 2;

        }

        //試合継続の判定，結果の表示
        public bool result()
        {
            if (judge() == 1)
            {
                label1.Text = "Player win!";
                disabled();
                Console.WriteLine("プレイヤーの勝ち");
                buttons[9].Enabled = true;
                return true;
            }
            else if (judge() == -1)
            {
                label1.Text = "Computer win...";
                disabled();
                Console.WriteLine("プレイヤーの負け");
                buttons[9].Enabled = true;
                return true;
            }else if(judge() == 0)
            {
                label1.Text = "This result is drow";
                Console.WriteLine("引き分け");
                buttons[9].Enabled = true;
                return true;
            }
            return false;
        }        

        //リプレイ
        private void button10_Click(object sender, EventArgs e)
        {
            Console.WriteLine("リプレイ");
            for (int i = 0; i < 9; i++)
            {
                buttons[i].Text = "blank";
            }
            buttons[10].Enabled = true;
            buttons[11].Enabled = true;
            label1.Text = "first move or second move?";
            Console.WriteLine("リプレイしました");
        }

        //先手ボタンクリック
        private void button11_Click(object sender, EventArgs e)
        {
            Console.WriteLine("先手を選択");
            label1.Text = "player is first move";
            for (int i = 0; i < 9; i++)
            {
                buttons[i].Enabled = true;
            }
            buttons[10].Enabled = false;
            buttons[11].Enabled = false;
        }

        //後手ボタンクリック
        private void button12_Click(object sender, EventArgs e)
        {
            Console.WriteLine("後手を選択");
            label1.Text = "computer is first move";
            for (int i = 0; i < 9; i++)
            {
                buttons[i].Enabled = true;
            }
            buttons[10].Enabled = false;
            buttons[11].Enabled = false;

            Random r = new System.Random();
            int r1 = r.Next(1, 9);

            buttons[r1].Text = "×";
            buttons[r1].Enabled = false;
            Console.WriteLine("コンピュータの一手");

        }
    }

}
