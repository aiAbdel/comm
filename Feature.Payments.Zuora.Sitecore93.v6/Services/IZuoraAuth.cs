using System.Threading.Tasks; namespace Your.Feature.Payments.Services { public interface IZuoraAuth { System.Threading.Tasks.Task<string> GetTokenAsync(); } }
