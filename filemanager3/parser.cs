using System;
using System.Collections.Generic;

namespace filemanager3
{
    class ParserException : ApplicationException
    {
        public ParserException(string str) : base(str) { }
        public override string ToString()
        { return Message; }
    }
    public class Parser
    {
        public Cell[,] Var;
        public int rows;
        public int columns;
        public static List<string> varsInFormula = new List<string>();
        public bool Error;
        public string message;
        enum Errors { SYNTAX, UNBALPARENS, NOEXP, DIVBYZERO, RECURSIVE, OUTOFRANGE };
        public Parser(Cell[,] vars,int rows,int columns)
        {
            Var = vars;
            this.rows = rows;
            this.columns = columns;
        }
        public double Eval(string exp)
        {
            Error = false;
            try
            {
                exp = RemoveWhite(exp);
                exp = SubstituteMetasymbols(exp);
                return Eval2(exp);
                //return 14.6;
            }
            catch(ParserException ex)
            {
                Console.WriteLine(ex);
                return 0.0;
            }
            //SyntaxErr(Errors.NOEXP);
        }
        void SyntaxErr(Errors error)
        {
            string[] err ={
                         "Синтаксична помилка",
                         "Дизбаланс дужок",
                         "Вираз відсутній",
                         "Ділення на нуль",
                         "Рекурсивне обчислення",
                         "Не існує клітинки з заданим індексом"};
            Error = true;
            message = err[(int)error];
            varsInFormula.Clear();
            throw new ParserException(err[(int)error]);
        }
        public double EvalSimpleValues(string exp)
        {
            double res;
            if (Double.TryParse(exp, out res))
                return res;
            else 
                if (IsVar(exp))
                    return ParseVar(exp);
            else SyntaxErr(Errors.SYNTAX);
            return 0.0;
        }
        public bool IsVar(string exp)
        {
            int count = 0;
            if (exp == "")
            {
                //SyntaxErr(Errors.NOEXP);
                return false;
            }
            if (exp[0] == 'R' && Char.IsDigit(exp[1]) && exp.Contains("C"))
            {
                for (int i = 0; i < exp.Length; i++)
                    if (Char.IsDigit(exp[i])) count++;
            }
            else return false;
                   
            return ( count == exp.Length-2);
        }
        public double ParseVar(string exp)
        {
            foreach (string s in varsInFormula)
                if (s == exp)
                {
                    SyntaxErr(Errors.RECURSIVE);
                    return 0;
                }
            varsInFormula.Add(exp);
            int i = 1;
            while (Char.IsDigit(exp[i]))
                i++;
            int X;
            int.TryParse(exp.Substring(1, i - 1), out X);
            int Y;
            int.TryParse(exp.Substring(i + 1, exp.Length - i - 1), out Y);
            if (X - 1 >= columns || Y - 1 >= rows)
            {
                SyntaxErr(Errors.OUTOFRANGE);
                return 0;
            }
            try
            {
                return Eval(Var[X - 1, Y - 1].text);
            }
            catch (Exception) {
                SyntaxErr(Errors.OUTOFRANGE);
                return 0;
            }
        }
        public double Eval0(string exp)
        {
            exp = RemoveBrackets(exp);
            double ret = 1;
            var list = MetaFirstPriorList(exp);
            if (list.Count != 0)
            {
                list.Add(exp.Length);
                for (int i = 1; i < list.Count; i++)
                {
                    switch (exp[list[i - 1]])
                    {
                        case '!':
                            ret = Eval2(exp.Substring(list[i - 1] + 1, list[i] - list[i - 1] - 1)) + 1;
                            break;
                        case '@':
                            ret = Eval2(exp.Substring(list[i - 1] + 1, list[i] - list[i - 1] - 1)) - 1;
                            break;
                    }
                }
            }
            else
            {
                ret = EvalSimpleValues(exp);
            }
            return ret;
        }
        public double Eval1(string exp)
        {
            exp = RemoveBrackets(exp);
            double ret = 1;
            var list = MetaSecondPriorList(exp);
            if (list.Count != 0)
            {
                list.Add(exp.Length);
                ret *= Eval2(exp.Substring(0, list[0]));
                for (int i = 1; i < list.Count; i++)
                {
                    double val = Eval2(exp.Substring(list[i - 1] + 1, list[i] - list[i - 1] - 1));
                    switch (exp[list[i - 1]])
                    {
                        case '*':
                            ret *= val;
                            break;
                        case '/':
                            if (val != 0) ret /= val;
                            else
                            {
                                SyntaxErr(Errors.DIVBYZERO);
                                return 0;
                            }
                            break;
                        case '%':
                            if (val != 0) ret %= val;
                            else
                            {
                                SyntaxErr(Errors.DIVBYZERO);
                                return 0;
                            }
                            break;
                        case '#':
                            if (val != 0) ret = (double)((int)(ret / val));
                            else
                            {
                                SyntaxErr(Errors.DIVBYZERO);
                                return 0;
                            }
                            break;
                    }
                }
            }
            else
            {
                ret = Eval0(exp);
            }
            return ret;
        }
        public double Eval2(string exp)
        {
            exp = RemoveBrackets(exp);
            double ret = 0;
            var list = MetaPlusMinusList(exp);
            if (list.Count != 0) 
            {
                list.Add(exp.Length);
                if (list[0]!=0)
                    ret += Eval2(exp.Substring(0, list[0]));
                for (int i = 1; i < list.Count; i++)
                {
                    switch (exp[list[i - 1]])
                    {
                        case '+':
                            ret += Eval2(exp.Substring(list[i - 1] + 1, list[i] - list[i - 1] - 1));
                            break;
                        case '-':
                            ret -= Eval2(exp.Substring(list[i - 1] + 1, list[i] - list[i - 1] - 1));
                            break;
                    }
                }
            }
            else ret = Eval1(exp);
            return ret;
        }
        private List<int> MetaPlusMinusList(string exp)
        {
            int leftb = 0;
            int rightb = 0;
            var ret = new List<int>();
            for (int i = 0; i < exp.Length; i++)
            {
                if ((exp[i] == '-' || exp[i] == '+') && (leftb == rightb))
                    ret.Add(i);
                if (exp[i] == '(') leftb++;
                if (exp[i] == ')') rightb++;
            }
            return ret;
        }
        private List<int> MetaSecondPriorList(string exp)
        {
            int leftb = 0;
            int rightb = 0;
            var ret = new List<int>();
            for (int i = 0; i < exp.Length; i++)
            {
                if ((exp[i] == '/' || exp[i] == '*' || exp[i] == '%' || exp[i] == '#') && (leftb == rightb))
                    ret.Add(i);
                if (exp[i] == '(') leftb++;
                if (exp[i] == ')') rightb++;
            }
            return ret;
        }
        private List<int> MetaFirstPriorList(string exp)
        {
            int leftb = 0;
            int rightb = 0;
            var ret = new List<int>();
            for (int i = 0; i < exp.Length; i++)
            {
                if ((exp[i] == '!' || exp[i] == '@') && (leftb == rightb))
                    ret.Add(i);
                if (exp[i] == '(') leftb++;
                if (exp[i] == ')') rightb++;
                if (leftb < rightb) SyntaxErr(Errors.UNBALPARENS);

            }
            return ret;
        }
        private string SubstituteMetasymbols(string exp)
        {
            exp = exp.Replace("mod", "%");
            exp = exp.Replace("div", "#");
            exp = exp.Replace("inc", "!");
            exp = exp.Replace("dec", "@");
            return exp;
        }
        private string RemoveBrackets(string exp)
        {
            string ret = exp;
            if (exp != "")
            {
                string left = exp[0].ToString();
                string right = exp[exp.Length - 1].ToString();
                while (left == "(" && right == ")" && !ExistsMetaSymbol(ret)&&(ret!=""))
                {
                    ret = ret.Remove(0, 1);
                    ret = ret.Remove(ret.Length - 1);
                    if (ret != "")
                    {
                        left = ret[0].ToString();
                        right = ret[ret.Length - 1].ToString();
                    }
                }
            }
            return ret;
        }
        private bool ExistsMetaSymbol(string exp)
        {
            int count = -1;
            int leftb = 0;
            int rightb = 0;
            for (int i = 0; i < exp.Length; i++)
            {
                if (exp[i] == '(') leftb++;
                if (exp[i] == ')') rightb++;
                if (rightb == leftb) count++;
            }
            return count  > 0;
        }
        private string RemoveWhite(string exp)
        {
            return exp.Replace(" ", "");
        }
    }
}
