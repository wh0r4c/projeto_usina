# --- Estágio 1: O "Build" (Compilação) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o ficheiro .csproj (especificando o caminho completo)
COPY backend/UsinaApi/UsinaApi.csproj ./backend/UsinaApi/

# Restaura os pacotes para esse projeto
RUN dotnet restore ./backend/UsinaApi/UsinaApi.csproj

# Copia todo o resto do código-fonte
COPY . .

# Publica o projeto (especificando o caminho completo)
# NOTA: O WORKDIR continua a ser /src
RUN dotnet publish ./backend/UsinaApi/UsinaApi.csproj -c Release -o /app/publish

# --- Estágio 2: O "Runtime" (Execução) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# O comando para iniciar (que já tínhamos)
ENTRYPOINT ["sh", "-c", "dotnet ef database update && dotnet UsinaApi.dll"]