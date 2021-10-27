namespace VdfConverter.Enums
{
    /// <summary>
    /// https://developer.valvesoftware.com/wiki/KeyValues#About_KeyValues_Text_File_Format:
    /// </summary>
    public enum WhitespaceCharacters
    {
        Space = ' ',
        Tab = 9,
        NewLine = 10, // \n
        CarriageReturn = 13 // \r
    }

    /// <summary>
    /// https://developer.valvesoftware.com/wiki/KeyValues#About_KeyValues_Text_File_Format:
    /// </summary>
    public enum ControlCharacters
    {
        Quote = '"',
        OpenBrace = '{',
        CloseBrace = '}',
        BackSlash = '\\'
    }

}
