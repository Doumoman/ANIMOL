using System.Collections.Generic;
using UnityEngine;

namespace ANIMOL.Puzzle
{
    // Authoring/layout operation only. Animation never moves this frame or its pillars.
    public sealed class MemorySkyTheaterFrame : MonoBehaviour
    {
        public SpriteRenderer right;
        public List<SpriteRenderer> lintels = new List<SpriteRenderer>();
        public void SetWidth(int cells)
        {
            cells = Mathf.Max(3, cells); var count = cells - 2;
            while (lintels.Count < count) lintels.Add(Instantiate(lintels[0], transform));
            for (var k = 0; k < lintels.Count; k++)
            { lintels[k].gameObject.SetActive(k < count); lintels[k].transform.localPosition = new Vector3(1.5f + k, 0, 0); }
            right.transform.localPosition = new Vector3(cells - .5f, -2, 0);
        }
    }
}
