namespace Email;

static class EmailTemplate
{
    public const string Welcome =
        """
        <!doctype html>
        <html lang="en">

        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width,initial-scale=1" />
          <title>Welcome to HelpDesk</title>
        </head>

        <body style="margin:0;padding:0;background:#ffffff;">
          <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="background:#ffffff;">
            <tr>
              <td align="left" style="padding:24px 12px;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                  style="max-width:850px;border-collapse:collapse;">
                  <tr>
                    <td style="
                          font-family: Arial, Helvetica, sans-serif;
                          font-size: 15px;
                          line-height: 1.6;
                          color: #202124;
                        ">
                      <p style="margin:0 0 16px 0;">Hi {{DisplayName}},</p>

                      <p style="margin:0 0 16px 0;">
                        Welcome to HelpDesk. Please verify your email address to activate your profile.
                      </p>

                      <p style="margin:0 0 16px 0;">
                        <a href="{{VerificationLink}}">Verify your profile</a>
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>

        </html>
        """;

    public const string PasswordReset =
        """
        <!doctype html>
        <html lang="en">

        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width,initial-scale=1" />
          <title>Reset your HelpDesk password</title>
        </head>

        <body style="margin:0;padding:0;background:#ffffff;">
          <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="background:#ffffff;">
            <tr>
              <td align="left" style="padding:24px 12px;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%"
                  style="max-width:850px;border-collapse:collapse;">
                  <tr>
                    <td style="
                          font-family: Arial, Helvetica, sans-serif;
                          font-size: 15px;
                          line-height: 1.6;
                          color: #202124;
                        ">
                      <p style="margin:0 0 16px 0;">Hi {{DisplayName}},</p>

                      <p style="margin:0 0 16px 0;">
                        We received a request to reset your HelpDesk password. Use the link below to choose a new one.
                        This link expires in 30 minutes.
                      </p>

                      <p style="margin:0 0 16px 0;">
                        <a href="{{ResetLink}}">Reset your password</a>
                      </p>

                      <p style="margin:0 0 16px 0;">
                        If you did not request a password reset, you can ignore this email.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>

        </html>
        """;
}
