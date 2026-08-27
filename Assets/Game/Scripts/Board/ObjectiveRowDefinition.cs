using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Gameplay
{
    [Serializable]
    public sealed class ObjectiveRowDefinition
    {
        [SerializeField] private BeatColor[] notes = Array.Empty<BeatColor>();

        public IReadOnlyList<BeatColor> Notes => notes;
        public int NoteCount => notes?.Length ?? 0;

        public ObjectiveRowDefinition(params BeatColor[] sourceNotes)
        {
            notes = sourceNotes != null ? (BeatColor[])sourceNotes.Clone() : Array.Empty<BeatColor>();
        }

        public ObjectiveRowDefinition Copy()
        {
            return new ObjectiveRowDefinition(notes);
        }
    }
}
