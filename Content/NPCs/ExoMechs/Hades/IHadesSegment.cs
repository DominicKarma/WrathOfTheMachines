namespace WoTM.Content.NPCs.ExoMechs
{
    public interface IHadesSegment
    {
        /// <summary>
        /// How open this segment is.
        /// </summary>
        public float SegmentOpenInterpolant
        {
            get;
            set;
        }

        /// <summary>
        /// The index to the ahead segment in the NPC array.
        /// </summary>
        public int AheadSegmentIndex
        {
            get;
        }
    }
}
