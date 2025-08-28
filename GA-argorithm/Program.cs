using System;
using MyGAEnvironments;

class Program
{
    static void Main(string[] args)
    {
        int sum = 0;
        //for (int a = 0; a < 100; a++) //100試行の為のfor文
        //{
        Random r = new System.Random();
        int[][] group = new int[5][];       //集団
        int[][] child = new int[5][];       //次世代の子供たち


        //初期集団生成
        for (int i = 0; i < group.Length; i++)
        {
            group[i] = new int[10];
            for (int j = 0; j < group[i].Length; j++)
            {
                group[i][j] = r.Next(0, 2);
            }
        }

        int gen = 0;                    //世代数カウント
        double score = 0;               //適合度
        double oya1 = 0;                //最優秀親のスコア
        double oya2 = 0;                //準優秀親のスコア
        int[] x = new int[10];          //最優秀親の個体値
        int[] y = new int[10];          //準優秀親の個体値
        int[] n = new int[10];          //遺伝用配列
        int cut = 0;                    //一点交叉の切れ目
        Boolean unfit = true;           //100点が出たらfalseに

        while (unfit)
        {      //理想個体出たらbreak文で終了
            gen++;  //世代数加算
            double ave = 0;

            System.Diagnostics.Debug.WriteLine(gen);
            System.Diagnostics.Debug.Write("oya1:" + String.Join("", x) + ":");
            System.Diagnostics.Debug.WriteLine("oya2:" + String.Join("", y) + ":");    //世代数&親の個体値表示

            for (int i = 0; i < group.GetLength(0); i++)
            {
                score = MyGAEnvironments.Environment3.fitness(String.Join("", group[i]));   //個体値ジャッジ

                ave += score;

                System.Diagnostics.Debug.Write(String.Join("", group[i]) + ":");
                System.Diagnostics.Debug.WriteLine(score);   //個体値とその点数を表示
                if (score == 100) unfit = false;    //理想個体が出たら厳選終了

                if (oya2 < score)
                {//準優秀個体よりでかい
                    if (oya1 >= score)
                    {//最優秀個体より雑魚
                        oya2 = score;
                        y = group[i];
                    }
                    else
                    {  //暫定最強なので最優秀個体更新
                        oya1 = score;
                        x = group[i];
                    }
                }


                System.Diagnostics.Debug.Write("oya1:" + String.Join("", x) + ":");
                System.Diagnostics.Debug.WriteLine("oya2:" + String.Join("", y) + ":");
            }
            System.Diagnostics.Debug.WriteLine("この世代の個体値の平均；" + String.Join("", ((double)ave / 5)));

            System.Diagnostics.Debug.Write("oya1:" + String.Join("", x) + ":");
            System.Diagnostics.Debug.WriteLine("oya2:" + String.Join("", y) + ":");

            for (int i = 0; i < child.Length; i++)
            {
                child[i] = new int[10];
            }

            for (int i = 0; i < 5; i++)
            {
                cut = r.Next(0, 10);                //1点交叉の交叉点決定
                n = x;                              //遺伝用配列に最優秀個体の個体値を代入
                for (int j = 0; j < 10; j++)
                {
                    if (j > cut)
                    {
                        n = y;                      //交叉点を超えたら遺伝用配列に準優秀個体値を代入
                    }
                    if (r.Next(0, 100) < 97)
                    {      //97%で通常遺伝
                        child[i][j] = n[j];
                    }
                    else
                    {                        //3%で突然変異(0と1を反転して遺伝)
                        if (n[j] == 0)
                        {
                            child[i][j] = 1;
                        }
                        else
                        {
                            child[i][j] = 0;
                        }
                    }
                }
            }

            group = child;                          //子どもたちは親に，，，
            System.Diagnostics.Debug.Write("oya1:" + String.Join("", x) + ":");
            System.Diagnostics.Debug.WriteLine("oya2:" + String.Join("", y) + ":");
        }
        System.Diagnostics.Debug.WriteLine("last:" + gen);
        sum += gen;
        //}
        System.Diagnostics.Debug.WriteLine(String.Join("", ((double)sum / 100)));//代表1試行の課題のための表示
    }
}