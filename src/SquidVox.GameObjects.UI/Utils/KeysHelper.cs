using Microsoft.Xna.Framework.Input;

namespace SquidVox.GameObjects.UI.Utils;

/// <summary>
/// Helper class for converting Keys to characters
/// </summary>
internal static class KeysHelper
{
    public static char? GetCharFromKey(Keys key, bool isShiftPressed)
    {
        return key switch
        {
            Keys.A => isShiftPressed ? 'A' : 'a',
            Keys.B => isShiftPressed ? 'B' : 'b',
            Keys.C => isShiftPressed ? 'C' : 'c',
            Keys.D => isShiftPressed ? 'D' : 'd',
            Keys.E => isShiftPressed ? 'E' : 'e',
            Keys.F => isShiftPressed ? 'F' : 'f',
            Keys.G => isShiftPressed ? 'G' : 'g',
            Keys.H => isShiftPressed ? 'H' : 'h',
            Keys.I => isShiftPressed ? 'I' : 'i',
            Keys.J => isShiftPressed ? 'J' : 'j',
            Keys.K => isShiftPressed ? 'K' : 'k',
            Keys.L => isShiftPressed ? 'L' : 'l',
            Keys.M => isShiftPressed ? 'M' : 'm',
            Keys.N => isShiftPressed ? 'N' : 'n',
            Keys.O => isShiftPressed ? 'O' : 'o',
            Keys.P => isShiftPressed ? 'P' : 'p',
            Keys.Q => isShiftPressed ? 'Q' : 'q',
            Keys.R => isShiftPressed ? 'R' : 'r',
            Keys.S => isShiftPressed ? 'S' : 's',
            Keys.T => isShiftPressed ? 'T' : 't',
            Keys.U => isShiftPressed ? 'U' : 'u',
            Keys.V => isShiftPressed ? 'V' : 'v',
            Keys.W => isShiftPressed ? 'W' : 'w',
            Keys.X => isShiftPressed ? 'X' : 'x',
            Keys.Y => isShiftPressed ? 'Y' : 'y',
            Keys.Z => isShiftPressed ? 'Z' : 'z',
            Keys.D0 => isShiftPressed ? ')' : '0',
            Keys.D1 => isShiftPressed ? '!' : '1',
            Keys.D2 => isShiftPressed ? '@' : '2',
            Keys.D3 => isShiftPressed ? '#' : '3',
            Keys.D4 => isShiftPressed ? '$' : '4',
            Keys.D5 => isShiftPressed ? '%' : '5',
            Keys.D6 => isShiftPressed ? '^' : '6',
            Keys.D7 => isShiftPressed ? '&' : '7',
            Keys.D8 => isShiftPressed ? '*' : '8',
            Keys.D9 => isShiftPressed ? '(' : '9',
            Keys.Space => ' ',
            Keys.OemPeriod => isShiftPressed ? '>' : '.',
            Keys.OemComma => isShiftPressed ? '<' : ',',
            Keys.OemQuestion => isShiftPressed ? '?' : '/',
            Keys.OemSemicolon => isShiftPressed ? ':' : ';',
            Keys.OemQuotes => isShiftPressed ? '"' : '\'',
            Keys.OemOpenBrackets => isShiftPressed ? '{' : '[',
            Keys.OemCloseBrackets => isShiftPressed ? '}' : ']',
            Keys.OemPipe => isShiftPressed ? '|' : '\\',
            Keys.OemTilde => isShiftPressed ? '~' : '`',
            Keys.OemMinus => isShiftPressed ? '_' : '-',
            Keys.OemPlus => isShiftPressed ? '+' : '=',
            _ => null
        };
    }
}
