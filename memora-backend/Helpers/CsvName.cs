using CsvHelper.Configuration.Attributes;

namespace CsvParallel;

class Person
{
    [Index(0)]
    public int Id { get; set; }
    [Index(1)]
    public string State { get; set; } = null!;
    [Index(2)]
    public string Sex { get; set; } = null!;
    [Index(3)]
    public int Year { get; set; }
    [Index(4)]
    public string Name { get; set; } = null!;
    [Index(5)]
    public string Count { get; set; } = null!;
}