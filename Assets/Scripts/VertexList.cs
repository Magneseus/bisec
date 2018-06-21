using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexList : IEnumerable
{
    private LinkedList<vertex_state> vertices;

    public enum vertex_flags
    {
        NONE,
        DEL,
        TRANSLATE,
        ADD_START,
        ADD_END
    }

    public struct vertex_state
    {
        public int vertex_flag;
        public Vector3 vertex;
    }


    public IEnumerator GetEnumerator()
    {
        throw new NotImplementedException();
    }
}
