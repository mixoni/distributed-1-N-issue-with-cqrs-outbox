namespace BuildingBlocks.Contracts;
public record CustomerDto(int Id, string Name);
public record CustomersBatchRequest(int[] Ids);
