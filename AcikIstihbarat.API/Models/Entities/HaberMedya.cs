namespace AcikIstihbarat.API.Models.Entities
{
    public class HaberMedya
    {
        public int HaberId { get; set; }
        public Haber Haber { get; set; }

        public int MedyaId { get; set; }
        public Medya Medya { get; set; }

        public bool OncuResimMi { get; set; }
        
        public int Sira { get; set; }
    }
}
