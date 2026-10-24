namespace Frameset.Office.Word.Element
{
    public class LsdException
    {
        private string name = string.Empty;
        private string qFormat = string.Empty;
        private string semiHidden = string.Empty;
        private string unhideWhenUsed = string.Empty;
        private string uiPriority = string.Empty;
        private LsdException()
        {

        }
        public class Builder
        {
            private static LsdException e = new LsdException();
            private Builder()
            {

            }
            public static Builder newBuilder()
            {
                return new Builder();
            }
            public Builder Name(string name)
            {
                e.name = name;
                return this;
            }
            public Builder QFormat(string qFormat)
            {
                e.qFormat = qFormat;
                return this;
            }
            public Builder SemiHidden(string semiHidden)
            {
                e.semiHidden = semiHidden;
                return this;
            }
            public Builder UnhideWhenUsed(string unhideWhenUsed)
            {
                e.unhideWhenUsed = unhideWhenUsed;
                return this;
            }
            public Builder UiPriority(string uiPriority)
            {
                e.uiPriority = uiPriority;
                return this;
            }
            public LsdException Build()
            {
                return e;
            }
        }

    }
}
