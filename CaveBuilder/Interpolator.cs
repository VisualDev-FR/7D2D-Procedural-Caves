public class Interpolator
{
    public int start;

    public int target;

    public int size;

    public Interpolator(int start, int target, int size)
    {
        this.start = start;
        this.target = target;
        this.size = size;
    }


    private int Interpolate(int index)
    {
        return start + index * (target - start) / size;
    }

    public int GetValueAt(int index)
    {
        return Interpolate(index);
    }
}