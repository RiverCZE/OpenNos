//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré à partir d'un modèle.
//
//     Des modifications manuelles apportées à ce fichier peuvent conduire à un comportement inattendu de votre application.
//     Les modifications manuelles apportées à ce fichier sont remplacées si le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OpenNos.DAL.EF.MySQL.DB
{
    using System;
    using System.Collections.Generic;
    
    public partial class inventory
    {
        public int Id { get; set; }
        public string Character { get; set; }
        public string Slot { get; set; }
        public string Pos { get; set; }
        public int characterCharacterId { get; set; }
    
        public virtual character character { get; set; }
        public virtual items items { get; set; }
    }
}
