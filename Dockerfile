# --- Estágio 1: O "Build" (Compilação) ---
# (Esta parte continua igual)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# --- Estágio 2: O "Runtime" (Execução) ---
# MUDANÇA AQUI: Trocamos "aspnet:8.0" por "sdk:8.0"
# Precisamos da imagem "sdk" completa porque ela contém 
# as ferramentas "dotnet ef" que precisamos para a migração.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# MUDANÇA AQUI: O novo ENTRYPOINT
# Esta linha diz ao Render para:
# 1. Rodar o "database update" (que vai criar o banco na primeira vez)
# 2. E ENTÃO ("&&") rodar o app principal "UsinaApi.dll"
ENTRYPOINT ["sh", "-c", "dotnet ef database update && dotnet UsinaApi.dll"]