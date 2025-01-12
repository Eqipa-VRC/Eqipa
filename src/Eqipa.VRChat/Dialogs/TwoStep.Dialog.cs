using Eqipa.Util;

namespace Eqipa.VRChat.Dialogs;

public class TwoStepDialog : Dialog
{
  public static readonly string _ENTER_CODE = "Enter code";

  private protected TextBox _code = new()
  {
    Text = _ENTER_CODE,
    Width = 200,
    Location = new(50, 30),
    ForeColor = Color.Gray
  };

  private protected Button _submit = new()
  {
    Text = "Verify",
    Location = new(110, 70),
    DialogResult = DialogResult.OK
  };

  private void Bind()
  {
    _code.Enter += (sender, e) =>
    {
      if (_code.Text == _ENTER_CODE)
      {
        _code.Text = "";
        _code.ForeColor = Color.Black;
      }
    };

    _code.Leave += (sender, e) =>
    {
      if (string.IsNullOrWhiteSpace(_code.Text))
      {
        _code.Text = _ENTER_CODE;
        _code.ForeColor = Color.Gray;
      }
    };
  }

  public TwoStepDialog(bool mail) : base()
  {
    Text = $"{(mail ? "Email" : "2FA")} verification code required";
    Width = 300;
    Height = 150;

    Bind();
    Controls.Add(_code);
    Controls.Add(_submit);
  }
}