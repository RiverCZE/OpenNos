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
    
    public partial class warehouse
    {
        public int id { get; set; }
        public int Owner { get; set; }
        public int Item { get; set; }
        public int Pos { get; set; }
    
        public virtual account account { get; set; }
        public virtual family family { get; set; }
    }
}
