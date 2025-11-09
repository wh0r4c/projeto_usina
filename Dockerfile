# --- Estágio 1: O "Build" (Compilação) ---
# Usamos a imagem completa do .NET SDK para compilar o app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o .csproj e restaura os pacotes (BCrypt, Npgsql, etc.)
# Isso é feito primeiro para aproveitar o cache do Docker
COPY *.csproj .
RUN dotnet restore

# Copia todo o resto do código-fonte e compila
COPY . .
RUN dotnet publish -c Release -o /app/publish

# --- Estágio 2: O "Runtime" (Execução) ---
# Agora usamos a imagem "aspnet", que é muito menor e mais leve,
# apenas para *rodar* o app que já foi compilado.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Define o comando que o Render vai usar para iniciar o app
ENTRYPOINT ["dotnet", "UsinaApi.dll"]