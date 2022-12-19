namespace TalkBox
{
    interface ILineOutput
    {
        public void Start();
        public void DisplayLine(TLine line);
        public void DisplayOptions(TLine[] options);
        public void OptionSelected();
        public void End();
    }
}