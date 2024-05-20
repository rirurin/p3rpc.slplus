namespace p3rpc.slplus.Interfaces;

public interface ICommuListColors
{
    public void SetBgColorNormal(byte r, byte g, byte b, byte a);
    public void SetBgColorSelected(byte r, byte g, byte b, byte a);
    public void SetFgColorNormal(byte r, byte g, byte b, byte a);
    public void SetFgColorReverse(byte r, byte g, byte b, byte a);
    public void SetFgColorSelected(byte r, byte g, byte b, byte a);
    public void SetCursorColor(byte r, byte g, byte b, byte a);
    public void SetListTitleColor(byte r, byte g, byte b, byte a);
    public void SetDetailsBgBox(byte r, byte g, byte b, byte a);
    public void SetDetailsNameplateTriangle(byte r, byte g, byte b, byte a);
    public void SetDetailsNameSprite(byte r, byte g, byte b, byte a);
}
