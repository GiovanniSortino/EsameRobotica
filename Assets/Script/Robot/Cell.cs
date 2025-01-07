public class Cell{
    public int x, y;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Cell parent;

    public Cell(int x, int y){
        this.x = x;
        this.y = y;
    }
}