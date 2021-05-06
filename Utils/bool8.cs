using System;

/* *****
 * bool8 datatype
 * propose: store up to 8 bool values in 1 byte instead of 8 bools in 8 seperate bytes
 * bool order 0 to 7: 8b76543210
 * worst case example: 
 * 1000 clients sending a bool for every key on a keyboard 60 times a second to server
 * with bool: 104 * 60 * 1000 	 = 6240000 B/s = 6,24 MB/s
 * with bool8: 104/8 * 60 * 1000 = 780000 B/s  = 0,78 MB/s
 * *****/

public class bool8
{
    public byte value = 0;

    public static const byte FALSE = 0;
    public static const byte TRUE = 255;

    public bool8() {}

    public bool8(
        bool bool0,
        bool bool1 = false,
        bool bool2 = false,
        bool bool3 = false,
        bool bool4 = false,
        bool bool5 = false,
        bool bool6 = false, 
        bool bool7 = false)
    {
        if (bool0) { this.value |= 1 << 0; }
        if (bool1) { this.value |= 1 << 1; }
        if (bool2) { this.value |= 1 << 2; }
        if (bool3) { this.value |= 1 << 3; }
        if (bool4) { this.value |= 1 << 4; }
        if (bool5) { this.value |= 1 << 5; }
        if (bool6) { this.value |= 1 << 6; }
        if (bool7) { this.value |= 1 << 7; }
    }

    public bool8(bool[] boolArray)
    {
        int index = 0;
        for (bool boolValue in boolArray)
        {
            if (boolValue) { this.value |= 1 << index; }
            index++;
        }
    }

    public bool8(bool8 other)
    {
        this.value = other.value;
    }

    public bool8(byte b)
    {
        this.value = b;
    }
 
    public int ToInt32()
    {
        return (int)this.value;
    }
 
    public override string ToString()
    {
        return "8b" + Convert.ToString(this.value, 2).PadLeft(8, '0');
    }
 
    public static bool8 AND(bool8 a, bool8 b)
    {
        return new bool8(a.value & b.value);
    }

    public static bool8 OR(bool8 a, bool8 b)
    {
        return new bool8(a.value | b.value);
    }

    public static bool8 XOR(bool8 a, bool8 b)
    {
        return new bool8(a.value ^ b.value);
    }

    public static bool8 NOT(bool8 a)
    {
        return new bool8(~(a.value));
    }

    public bool Get(int index)
    {
        return (this.value & (1 << index)) != 0;
    }

    public bool[] Get()
    {
        return bool[] boolArray = new bool[8]
        {
            (this.value & (1 << 0)) != 0,
            (this.value & (1 << 1)) != 0,
            (this.value & (1 << 2)) != 0,
            (this.value & (1 << 3)) != 0,
            (this.value & (1 << 4)) != 0,
            (this.value & (1 << 5)) != 0,
            (this.value & (1 << 6)) != 0,
            (this.value & (1 << 7)) != 0
        };
    }

    public void Set(int index, bool boolValue = true)
    {
        if (boolValue) 
        {
            this.value |= 1 << index;
        } 
        else 
        {
            this.value = (byte)(this.value & ~(1 << index));
        }
    }

    public void Set(bool[] boolArray, bool fill = true)
    {
        int index = 0;
        for (bool boolValue in boolArray)
        {
            (boolValue) ? this.value |= 1 << index : (byte)(this.value & ~(1 << index));
            index++;
        }

        if (fill)
        {
            for (int i = index; i < 8; i++)
            {
                this.value = (byte)(this.value & ~(1 << i));
            }
        }
    }

    public void Set(bool8 other)
    {
        this.value = other.value;
    }

    public void ClearFalse()
    {
        this.value = FALSE;
    }

    public void ClearTrue()
    {
        this.value = TRUE;
    }
}
