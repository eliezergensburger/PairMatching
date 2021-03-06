using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DO
{
    public class LastDataOfSpredsheet : IEquatable<LastDataOfSpredsheet>
    {
        [BsonId]
        public int Id { get; } = 2;
        public DateTime HebrewSheets { get; set; }
        public DateTime EnglishSheets { get; set; }

        public bool Equals(LastDataOfSpredsheet other)
        {
            //Check whether the compared object is null.
            if (ReferenceEquals(other, null)) 
                return false;

            //Check whether the compared object references the same data.
            if (ReferenceEquals(this, other)) 
                return true;

            return HebrewSheets == other.HebrewSheets 
                && EnglishSheets == other.EnglishSheets;
        }

        public override bool Equals(object obj) => Equals(obj as LastDataOfSpredsheet);

        public override int GetHashCode() => (HebrewSheets, EnglishSheets).GetHashCode();
    }
}
