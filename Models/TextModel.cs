namespace porukica.Models
{
    public class TextModel : BaseModel
    {
        public TextModel(string secret, string text) : base(secret)
        {
            Text = text;
        }

        public string Text { get; set; }
    }
}