namespace Frameset.Office.Element
{
    public class Formula
    {
        string expression;
        public Formula(string expression)
        {
            this.expression = expression;
        }
        public string GetExpression()
        {
            return expression;
        }
        public void setExpression(string expression)
        {
            this.expression = expression;
        }
    }
}
