namespace Eqipa.Util;

public class Dialog : Form
{
  public Dialog() : base()
  {
    MinimizeBox = false;
    MaximizeBox = false;
    StartPosition = FormStartPosition.CenterScreen;
    FormBorderStyle = FormBorderStyle.FixedDialog;
  }
}