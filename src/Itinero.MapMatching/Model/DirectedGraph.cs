using System;
using System.Collections.Generic;

namespace Itinero.MapMatching.Model
{
    internal sealed class DirectedGraph
    {
        private uint[] _vertices = new uint[1024];
        private const uint NO_DATA = 0;
        private readonly List<uint> _edges = new List<uint>();
        
        public void AddEdge(uint vertex1, uint vertex2, uint data)
        {
            var vertexLength = _vertices.Length;
            while (vertex1 >= vertexLength)
            {
                vertexLength *= 2;
            }
            while (vertex2 >= vertexLength)
            {
                vertexLength *= 2;
            }

            if (vertexLength != _vertices.Length)
            {
                Array.Resize(ref _vertices, vertexLength);
            }
            
            var pointer = (uint)_edges.Count;
            _edges.Add(vertex1);
            _edges.Add(vertex2);
            _edges.Add(data);
            _edges.Add(_vertices[vertex1]);
            _edges.Add(_vertices[vertex2]);

            _vertices[vertex1] = pointer + 1;
            _vertices[vertex2] = pointer + 1;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public class Enumerator
        {
            private readonly DirectedGraph _graph;

            public Enumerator(DirectedGraph graph)
            {
                _graph = graph;
            }

            private uint _pointer = NO_DATA;
            private uint _vertex = NO_DATA;

            public void MoveTo(uint vertex)
            {
                _vertex = vertex;
                _pointer = _graph._vertices[vertex];
                if (_pointer != NO_DATA) _pointer -= 1;
            }

            public uint From { get; private set; }
            
            public uint To { get; private set; }
            
            public uint Data => _graph._edges[(int)_pointer + 2];

            public bool MoveNext()
            {
                if (_pointer == NO_DATA) return false;

                var vertex1 = _graph._edges[(int)_pointer];
                var vertex2 = _graph._edges[(int)_pointer + 1];
                if (vertex1 == _vertex)
                {
                    this.From = vertex1;
                    this.To = vertex2;
                    _pointer = _graph._edges[(int) _pointer + 3];
                    if (_pointer != NO_DATA) _pointer -= 1;
                    return true;
                }
                if (vertex2 == _vertex)
                {
                    this.From = vertex2;
                    this.To = vertex1;
                    _pointer = _graph._edges[(int) _pointer + 4];
                    if (_pointer != NO_DATA) _pointer -= 1;
                    return true;
                }

                return false;
            }
        }
    }
}