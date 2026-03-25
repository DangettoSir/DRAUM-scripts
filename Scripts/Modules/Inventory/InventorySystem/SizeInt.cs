using System.Collections;
using System.Collections.Generic;
using UnityEngine;

  [System.Serializable]
  public class SizeInt
  {
      public int width;
      public int height;
      public SizeInt() : this(0, 0) { }
      public SizeInt(int width, int height)
      {
          this.width = width;
          this.height = height;
      }
  }

