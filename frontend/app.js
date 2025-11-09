document.addEventListener('DOMContentLoaded', () => {

    // --- VARIÁVEIS E CONSTANTES ---
    // ATENÇÃO: Altere esta porta se o seu backend rodar em outra!
    // Você verá a porta correta no terminal ao rodar "dotnet run".
    const API_BASE_URL = 'http://localhost:5104'; // Pode ser 5123, 5001, 7001, etc.

    let currentToken = null;
    let textoParaFalar = '';

    // --- SELETORES DE ELEMENTOS ---
    const telaLogin = document.getElementById('tela-login');
    const telaPrincipal = document.getElementById('tela-principal');
    const loginForm = document.getElementById('login-form');
    const inputCpf = document.getElementById('cpf');
    const inputPin = document.getElementById('pin');
    const loginError = document.getElementById('login-error');

    const saudacao = document.getElementById('saudacao');
    const btnSair = document.getElementById('btn-sair');

    // ... (seletores do holerite) ...

    // Seletores do Modal RH
    const modalRh = document.getElementById('tela-rh-gravar');
    const btnFalarRh = document.getElementById('btn-falar-rh');
    const btnFecharRh = document.getElementById('btn-fechar-rh');
    const btnGravarAudio = document.getElementById('btn-gravar-audio');
    const btnPararAudio = document.getElementById('btn-parar-audio');
    const btnEnviarAudio = document.getElementById('btn-enviar-audio');
    const audioPreview = document.getElementById('audio-preview');
    const rhStatus = document.getElementById('rh-status');

    // Seletores do Modal Avisos
    const modalAvisos = document.getElementById('tela-avisos');
    const btnAvisos = document.getElementById('btn-avisos');
    const btnFecharAvisos = document.getElementById('btn-fechar-avisos');
    const listaAvisosContainer = document.getElementById('lista-avisos-container');
    const avisosStatus = document.getElementById('avisos-status');

    // Seletores do Modal FAQ
    const modalFaq = document.getElementById('tela-faq');
    const btnFaq = document.getElementById('btn-faq');
    const btnFecharFaq = document.getElementById('btn-fechar-faq');
    const listaFaqContainer = document.getElementById('lista-faq-container');
    const faqStatus = document.getElementById('faq-status');

    // Variáveis para o gravador
    let mediaRecorder; // O objeto gravador
    let audioChunks = []; // Um "balde" para os pedaços de áudio
    let audioBlob = null; // O arquivo de áudio final

    // Botões do Menu
    const btnHolerite = document.getElementById('btn-holerite');

    // Modal Holerite
    const modalHolerite = document.getElementById('tela-holerite-detalhe');
    const btnFecharHolerite = document.getElementById('btn-fechar-holerite');
    const valorHolerite = document.getElementById('holerite-valor-liquido');
    const btnOuvirHolerite = document.getElementById('btn-ouvir-holerite');
    const btnBaixarPdf = document.getElementById('btn-baixar-pdf');
    const holeriteError = document.getElementById('holerite-error');

    // Seletores do Modal Banco de Horas
    const modalBancoHoras = document.getElementById('tela-banco-horas');
    const btnBancoHoras = document.getElementById('btn-banco-horas');
    const btnFecharBancoHoras = document.getElementById('btn-fechar-banco-horas');
    const bancoHorasValor = document.getElementById('banco-horas-valor');
    const bancoHorasData = document.getElementById('banco-horas-data');
    const btnOuvirBancoHoras = document.getElementById('btn-ouvir-banco-horas');
    const bancoHorasStatus = document.getElementById('banco-horas-status');

    // --- FUNÇÕES DE LÓGICA ---

    // Mostra/esconde telas
    function mostrarTela(tela) {
        telaLogin.style.display = 'none';
        telaPrincipal.style.display = 'none';
        tela.style.display = 'block';
    }

    // Tenta fazer login
    async function fazerLogin(e) {
        e.preventDefault();
        loginError.textContent = '';

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    cpf: inputCpf.value,
                    pin: inputPin.value
                })
            });

            if (!resposta.ok) {
                throw new Error('CPF ou PIN inválidos.');
            }

            const dados = await resposta.json();
            currentToken = dados.token;
            sessionStorage.setItem('token', dados.token); // Salva o token na sessão

            saudacao.textContent = `Olá, ${dados.nome}!`;
            mostrarTela(telaPrincipal);

        } catch (err) {
            loginError.textContent = err.message;
        }
    }

    // Faz logout
    function fazerLogout() {
        currentToken = null;
        sessionStorage.removeItem('token');
        inputCpf.value = '';
        inputPin.value = '';
        mostrarTela(telaLogin);
    }

    // Tenta carregar o holerite
    async function carregarHolerite() {
        holeriteError.textContent = '';
        btnBaixarPdf.style.display = 'none';

        if (!currentToken) {
            holeriteError.textContent = 'Erro de autenticação. Tente logar novamente.';
            return;
        }

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/holerite`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${currentToken}`
                }
            });

            if (!resposta.ok) {
                throw new Error('Não foi possível carregar o holerite.');
            }

            const dados = await resposta.json();

            // Formata o valor como moeda (R$)
            const valorFormatado = dados.valor_liquido.toLocaleString('pt-BR', {
                style: 'currency',
                currency: 'BRL'
            });

            valorHolerite.textContent = valorFormatado;
            textoParaFalar = dados.texto_para_fala;

            if (dados.tem_pdf) {
                btnBaixarPdf.style.display = 'block';
            }

            modalHolerite.style.display = 'flex'; // Mostra o modal

        } catch (err) {
            holeriteError.textContent = err.message;
        }
    }

    // Baixa o PDF
    async function baixarPdf() {
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/holerite/pdf`, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${currentToken}`
                }
            });

            if (!resposta.ok) {
                throw new Error('Não foi possível baixar o PDF.');
            }

            // Converte a resposta em um "blob" (um arquivo)
            const blob = await resposta.blob();
            // Pega o nome do arquivo do cabeçalho (se o backend enviar)
            const contentDisposition = resposta.headers.get('content-disposition');
            let filename = 'holerite.pdf'; // Nome padrão
            if (contentDisposition) {
                const match = contentDisposition.match(/filename="?([^"]+)"?/);
                if (match) filename = match[1];
            }

            // Cria um link temporário na memória para fazer o download
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = filename; // O nome do arquivo
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            a.remove();

        } catch (err) {
            holeriteError.textContent = err.message;
        }
    }

    // --- FUNÇÕES DO MÓDULO RH ---

    function abrirModalRh() {
        // Reseta o modal para o estado inicial
        rhStatus.textContent = '';
        audioPreview.style.display = 'none';
        audioPreview.src = '';
        btnEnviarAudio.style.display = 'none';
        btnGravarAudio.style.display = 'block';
        btnPararAudio.style.display = 'none';
        btnGravarAudio.disabled = false;
        audioChunks = [];
        audioBlob = null;

        modalRh.style.display = 'flex';
    }

    async function iniciarGravacao() {
        rhStatus.textContent = 'Pedindo permissão para o microfone...';

        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

            // 1. Inicia o gravador
            mediaRecorder = new MediaRecorder(stream);

            // 2. Define o que fazer quando o gravador tiver dados
            mediaRecorder.ondataavailable = (event) => {
                audioChunks.push(event.data);
            };

            // 3. Define o que fazer quando a gravação PARAR
            mediaRecorder.onstop = () => {
                // Cria o arquivo de áudio final
                audioBlob = new Blob(audioChunks, { type: 'audio/wav' }); // Pode ser .wav ou .webm
                const audioUrl = URL.createObjectURL(audioBlob);

                // Mostra o "player" para o usuário ouvir
                audioPreview.src = audioUrl;
                audioPreview.style.display = 'block';

                // Habilita o botão de enviar
                btnEnviarAudio.style.display = 'block';
                btnEnviarAudio.disabled = false;
            };

            // 4. Muda os botões
            audioChunks = []; // Limpa o balde
            btnGravarAudio.style.display = 'none';
            btnPararAudio.style.display = 'block';
            rhStatus.textContent = 'Gravando... 🔴';

            // 5. Começa a gravar!
            mediaRecorder.start();

        } catch (err) {
            console.error('Erro ao acessar microfone:', err);
            rhStatus.textContent = 'Erro: Não foi possível acessar o microfone.';
        }
    }

    function pararGravacao() {
        if (mediaRecorder) {
            mediaRecorder.stop();

            // Para todas as faixas de áudio (desliga o ícone de microfone no navegador)
            mediaRecorder.stream.getTracks().forEach(track => track.stop());

            // Muda os botões
            btnPararAudio.style.display = 'none';
            btnGravarAudio.style.display = 'block';
            rhStatus.textContent = 'Gravação parada. Ouça e envie.';
        }
    }

    async function enviarGravacao() {
        if (!audioBlob || !currentToken) {
            rhStatus.textContent = 'Nenhum áudio para enviar ou erro de login.';
            return;
        }

        rhStatus.textContent = 'Enviando, por favor aguarde...';
        btnEnviarAudio.disabled = true;

        // FormData é a forma de enviar ARQUIVOS para uma API
        const formData = new FormData();
        // 'audioFile' DEVE ser o mesmo nome do parâmetro no C# (IFormFile audioFile)
        // 'gravacao.wav' é o nome do arquivo que o servidor verá.
        formData.append('audioFile', audioBlob, 'gravacao.wav');

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/suporte/audio`, {
                method: 'POST',
                headers: {
                    // NÃO defina 'Content-Type'. O navegador faz isso
                    // automaticamente (multipart/form-data) com o FormData.
                    'Authorization': `Bearer ${currentToken}`
                },
                body: formData
            });

            const dados = await resposta.json();

            if (!resposta.ok) {
                throw new Error(dados.message || 'Erro ao enviar áudio.');
            }

            rhStatus.textContent = `Enviado! Protocolo: ${dados.ticketId}`;
            btnEnviarAudio.disabled = true; // Desabilita após o envio

        } catch (err) {
            rhStatus.textContent = `Erro: ${err.message}`;
            btnEnviarAudio.disabled = false; // Permite tentar de novo
        }
    }

    async function carregarAvisos() {
        avisosStatus.textContent = 'Carregando avisos...';
        listaAvisosContainer.innerHTML = ''; // Limpa a lista antiga

        if (!currentToken) {
            avisosStatus.textContent = 'Erro de autenticação.';
            return;
        }

        modalAvisos.style.display = 'flex'; // Mostra o modal

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/avisos`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });

            if (!resposta.ok) {
                throw new Error('Não foi possível carregar os avisos.');
            }

            const avisos = await resposta.json();

            if (avisos.length === 0) {
                avisosStatus.textContent = 'Nenhum aviso no momento.';
                return;
            }

            avisosStatus.textContent = ''; // Limpa o status

            // Cria os cards para cada aviso
            avisos.forEach(aviso => {
                const card = document.createElement('div');
                card.className = 'aviso-card';

                // Usamos .innerText para prevenir XSS (segurança!)
                const titulo = document.createElement('h4');
                titulo.innerText = aviso.titulo;

                const conteudo = document.createElement('p');
                conteudo.innerText = aviso.conteudo;

                const footer = document.createElement('div');
                footer.className = 'aviso-card-footer';

                const data = document.createElement('span');
                data.className = 'data-aviso';
                data.innerText = `Publicado em: ${aviso.data}`;

                const btnOuvir = document.createElement('button');
                btnOuvir.className = 'btn-ouvir-aviso';
                btnOuvir.innerText = '▶️ Ouvir';

                // Usa uma função anônima para passar o texto para a função 'falarAviso'
                btnOuvir.onclick = () => {
                    // Passa o texto otimizado para fala
                    falarTexto(aviso.textoParaFala);
                };

                footer.appendChild(data);
                footer.appendChild(btnOuvir);

                card.appendChild(titulo);
                card.appendChild(conteudo);
                card.appendChild(footer);

                listaAvisosContainer.appendChild(card);
            });

        } catch (err) {
            avisosStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // Reutiliza a lógica de falar, mas de forma mais genérica
    function falarTexto(texto) {
        // Para qualquer fala anterior antes de começar uma nova
        window.speechSynthesis.cancel();

        if ('speechSynthesis' in window && texto) {
            const synth = window.speechSynthesis;
            const utterance = new SpeechSynthesisUtterance(texto);
            utterance.lang = 'pt-BR';
            synth.speak(utterance);
        } else {
            alert('Seu navegador não suporta a função de voz.');
        }
    }

    async function carregarFaq() {
        faqStatus.textContent = 'Carregando dúvidas...';
        listaFaqContainer.innerHTML = ''; // Limpa a lista antiga

        // NOTA: Não precisamos do token aqui!
        // A API é pública.

        modalFaq.style.display = 'flex'; // Mostra o modal

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/faq`, {
                method: 'GET'
                // Sem cabeçalho de Authorization
            });

            if (!resposta.ok) {
                throw new Error('Não foi possível carregar as dúvidas.');
            }

            const faqs = await resposta.json();

            if (faqs.length === 0) {
                faqStatus.textContent = 'Nenhuma dúvida cadastrada.';
                return;
            }

            faqStatus.textContent = ''; // Limpa o status

            // Cria os cards para cada FAQ
            faqs.forEach(faq => {
                const card = document.createElement('div');
                card.className = 'faq-card';

                // --- Pergunta (O "botão" do acordeão) ---
                const perguntaDiv = document.createElement('div');
                perguntaDiv.className = 'faq-pergunta';

                const titulo = document.createElement('h4');
                titulo.innerText = faq.pergunta;

                const iconeSeta = document.createElement('span');
                iconeSeta.innerText = '▼'; // Seta para baixo

                perguntaDiv.appendChild(titulo);
                perguntaDiv.appendChild(iconeSeta);

                // --- Resposta (Conteúdo escondido) ---
                const respostaDiv = document.createElement('div');
                respostaDiv.className = 'faq-resposta';

                const conteudo = document.createElement('p');
                conteudo.innerText = faq.resposta;

                const btnOuvir = document.createElement('button');
                btnOuvir.className = 'btn-ouvir-faq';
                btnOuvir.innerText = '▶️ Ouvir Resposta';
                btnOuvir.onclick = () => {
                    falarTexto(faq.textoParaFala);
                };

                respostaDiv.appendChild(conteudo);
                respostaDiv.appendChild(btnOuvir);

                // --- Montagem e Lógica do Acordeão ---
                card.appendChild(perguntaDiv);
                card.appendChild(respostaDiv);

                perguntaDiv.addEventListener('click', () => {
                    // Fecha todos os outros cards
                    document.querySelectorAll('.faq-card.active').forEach(item => {
                        if (item !== card) {
                            item.classList.remove('active');
                            item.querySelector('.faq-pergunta span').innerText = '▼';
                        }
                    });

                    // Abre ou fecha o card clicado
                    card.classList.toggle('active');
                    if (card.classList.contains('active')) {
                        iconeSeta.innerText = '▲'; // Seta para cima
                    } else {
                        iconeSeta.innerText = '▼'; // Seta para baixo
                        window.speechSynthesis.cancel(); // Para a fala se fechar
                    }
                });

                listaFaqContainer.appendChild(card);
            });

        } catch (err) {
            faqStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // --- FUNÇÕES DO MÓDULO BANCO DE HORAS ---
    async function carregarBancoHoras() {
        bancoHorasStatus.textContent = 'Carregando saldo...';

        if (!currentToken) {
            bancoHorasStatus.textContent = 'Erro de autenticação.';
            return;
        }

        modalBancoHoras.style.display = 'flex'; // Mostra o modal

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/bancohoras`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${currentToken}` }
            });

            if (!resposta.ok) {
                throw new Error('Não foi possível carregar o saldo.');
            }

            const dados = await resposta.json();

            bancoHorasStatus.textContent = ''; // Limpa o status

            bancoHorasValor.textContent = dados.horasFormatadas;
            bancoHorasData.textContent = `Atualizado em: ${dados.dataAtualizacao}`;

            // Configura o botão de ouvir
            btnOuvirBancoHoras.onclick = () => {
                falarTexto(dados.textoParaFala);
            };

        } catch (err) {
            bancoHorasStatus.textContent = `Erro: ${err.message}`;
        }
    }

    // Função de Falar (Text-to-Speech)
    function falar() {
        if ('speechSynthesis' in window && textoParaFalar) {
            const synth = window.speechSynthesis;
            const utterance = new SpeechSynthesisUtterance(textoParaFalar);
            utterance.lang = 'pt-BR';
            synth.speak(utterance);
        } else {
            alert('Seu navegador não suporta a função de voz.');
        }
    }


    // --- REGISTRO DE EVENTOS ---
    loginForm.addEventListener('submit', fazerLogin);
    btnSair.addEventListener('click', fazerLogout);

    // Botões do Menu
    btnHolerite.addEventListener('click', carregarHolerite);

    // Eventos do Modal Holerite
    btnFecharHolerite.addEventListener('click', () => {
        modalHolerite.style.display = 'none';
    });
    btnOuvirHolerite.addEventListener('click', falar);
    btnBaixarPdf.addEventListener('click', baixarPdf);

    // ... (eventos do modal holerite) ...

    // Eventos do Modal RH
    btnFalarRh.addEventListener('click', abrirModalRh);
    btnFecharRh.addEventListener('click', () => modalRh.style.display = 'none');
    btnGravarAudio.addEventListener('click', iniciarGravacao);
    btnPararAudio.addEventListener('click', pararGravacao);
    btnEnviarAudio.addEventListener('click', enviarGravacao);

    // Eventos do Modal Avisos
    btnAvisos.addEventListener('click', carregarAvisos);
    btnFecharAvisos.addEventListener('click', () => {
        modalAvisos.style.display = 'none';
        window.speechSynthesis.cancel(); // Para qualquer fala se o modal fechar
    });

    // Eventos do Modal FAQ
    btnFaq.addEventListener('click', carregarFaq);
    btnFecharFaq.addEventListener('click', () => {
        modalFaq.style.display = 'none';
        window.speechSynthesis.cancel(); // Para qualquer fala se o modal fechar
    });

    // Eventos do Modal Banco de Horas
    btnBancoHoras.addEventListener('click', carregarBancoHoras);
    btnFecharBancoHoras.addEventListener('click', () => {
        modalBancoHoras.style.display = 'none';
        window.speechSynthesis.cancel(); // Para a fala se fechar
    });

    // --- INICIALIZAÇÃO ---
    // Verifica se já existe um token na sessão ao carregar a página
    const tokenSalvo = sessionStorage.getItem('token');
    if (tokenSalvo) {
        // Se tem token, vamos pular o login
        // (Em um app real, você validaria esse token com a API)
        currentToken = tokenSalvo;
        saudacao.textContent = 'Olá!'; // Não temos o nome aqui, mas tudo bem
        mostrarTela(telaPrincipal);
    } else {
        mostrarTela(telaLogin);
    }
});
