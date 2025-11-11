document.addEventListener('DOMContentLoaded', () => {

    // --- Lógica "Inteligente" de Ambiente ---
    let API_BASE_URL;
    
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        API_BASE_URL = 'http://localhost:5104'; 
    } else {
        API_BASE_URL = 'https://projeto-usina.onrender.com';
    }

    let adminToken = null;
    let colaboradorEditandoId = null; // NOVO: Guarda o ID de quem estamos a editar

    // --- Seletores de Elementos ---
    const telaLoginAdmin = document.getElementById('tela-login-admin');
    const telaDashboardAdmin = document.getElementById('tela-dashboard-admin');
    
    const loginFormAdmin = document.getElementById('login-form-admin');
    const adminEmail = document.getElementById('admin-email');
    const adminPassword = document.getElementById('admin-password');
    const adminLoginError = document.getElementById('admin-login-error');
    
    const adminSaudacao = document.getElementById('admin-saudacao');
    const btnAdminSair = document.getElementById('btn-admin-sair');
    const bodyEl = document.querySelector('body');

    // --- Seletores do Painel ---
    const painelTitulo = document.getElementById('painel-titulo');
    
    // Contentores
    const listaColaboradoresContainer = document.getElementById('lista-colaboradores-container');
    const formColaboradorContainer = document.getElementById('form-colaborador-container');
    const listaAudiosContainer = document.getElementById('lista-audios-container');

    // Botões e Forms (Colaborador)
    const btnMostrarForm = document.getElementById('btn-mostrar-form-colaborador');
    const btnCancelarForm = document.getElementById('btn-cancelar-form-colaborador');
    const formNovoColaborador = document.getElementById('form-novo-colaborador');
    const formColaboradorStatus = document.getElementById('form-colaborador-status');

    // Navegação
    const navColaboradores = document.getElementById('nav-colaboradores');
    const navAudios = document.getElementById('nav-audios');
    
    // --- Seletores do Modal de Edição (NOVOS) ---
    const modalEditarEl = document.getElementById('modal-editar-colaborador');
    const bsModalEditar = new bootstrap.Modal(modalEditarEl);
    const editarModalTitulo = document.getElementById('editar-modal-titulo');
    
    const formEditarBancoHoras = document.getElementById('form-editar-banco-horas');
    const statusBancoHoras = document.getElementById('status-banco-horas');
    
    const formEditarFerias = document.getElementById('form-editar-ferias');
    const statusFerias = document.getElementById('status-ferias');
    
    const formEnviarHolerite = document.getElementById('form-enviar-holerite');
    const statusHolerite = document.getElementById('status-holerite');


    // --- Funções de Lógica ---

    function mostrarTela(telaParaMostrar) {
        if (telaParaMostrar === telaDashboardAdmin) {
            telaLoginAdmin.style.display = 'none';
            telaDashboardAdmin.style.display = 'block';
            bodyEl.classList.add('dashboard-mode');
            mostrarPainelColaboradores();
        } else {
            telaLoginAdmin.style.display = 'block';
            telaDashboardAdmin.style.display = 'none';
            bodyEl.classList.remove('dashboard-mode');
        }
    }

    async function fazerLoginAdmin(e) {
        e.preventDefault();
        adminLoginError.textContent = '';
        
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    email: adminEmail.value,
                    password: adminPassword.value
                })
            });
            if (!resposta.ok) throw new Error('Email ou Senha inválidos.');

            const dados = await resposta.json();
            adminToken = dados.token;
            sessionStorage.setItem('admin_token', dados.token); 
            adminSaudacao.textContent = `Olá, ${dados.nome}!`;
            mostrarTela(telaDashboardAdmin);
        } catch (err) {
            adminLoginError.textContent = err.message;
        }
    }

    function fazerLogoutAdmin() {
        adminToken = null;
        sessionStorage.removeItem('admin_token');
        adminEmail.value = '';
        adminPassword.value = '';
        mostrarTela(telaLoginAdmin);
    }

    // --- LÓGICA DE NAVEGAÇÃO DO PAINEL ---

    function mostrarPainelColaboradores() {
        painelTitulo.textContent = 'Gestão de Colaboradores';
        btnMostrarForm.style.display = 'block';
        listaAudiosContainer.style.display = 'none';
        listaColaboradoresContainer.style.display = 'block';
        formColaboradorContainer.style.display = 'none';
        navColaboradores.classList.add('active');
        navAudios.classList.remove('active');
        carregarColaboradores();
    }

    function mostrarPainelAudios() {
        painelTitulo.textContent = 'Áudios Pendentes';
        btnMostrarForm.style.display = 'none';
        listaAudiosContainer.style.display = 'block';
        listaColaboradoresContainer.style.display = 'none';
        formColaboradorContainer.style.display = 'none';
        navColaboradores.classList.remove('active');
        navAudios.classList.add('active');
        carregarAudios();
    }

    // --- FUNÇÕES DE DADOS DO PAINEL ADMIN ---

    // 1. LISTAR COLABORADORES
    async function carregarColaboradores() {
        if (!adminToken) return;
        listaColaboradoresContainer.innerHTML = '<p>A carregar colaboradores...</p>';
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${adminToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar os colaboradores.');
            const colaboradores = await resposta.json();
            desenharTabelaColaboradores(colaboradores);
        } catch (err) {
            listaColaboradoresContainer.innerHTML = `<p class="text-danger">${err.message}</p>`;
        }
    }

    function desenharTabelaColaboradores(colaboradores) {
        if (colaboradores.length === 0) {
            listaColaboradoresContainer.innerHTML = '<p>Nenhum colaborador encontrado.</p>';
            return;
        }
        let tabelaHtml = `<table class="table table-hover"><thead>...</thead><tbody>`;
        // Recria o cabeçalho
        tabelaHtml = `
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Nome</th>
                        <th>CPF</th>
                        <th>Matrícula</th>
                        <th>Status</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
        `;
        for (const colab of colaboradores) {
            const statusPin = colab.pinFoiDefinido 
                ? '<span class="badge bg-success">Ativo</span>' 
                : '<span class="badge bg-warning">Pendente</span>';
            tabelaHtml += `
                <tr data-id="${colab.id}" data-nome="${colab.nome}">
                    <td>${colab.id}</td>
                    <td>${colab.nome}</td>
                    <td>${colab.cpf}</td>
                    <td>${colab.matricula}</td>
                    <td>${statusPin}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary btn-acao btn-editar">Editar</button>
                    </td>
                </tr>
            `;
        }
        tabelaHtml += '</tbody></table>';
        listaColaboradoresContainer.innerHTML = tabelaHtml;

        // ** NOVO: Adiciona "listeners" aos botões "Editar" **
        document.querySelectorAll('.btn-editar').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tr = e.target.closest('tr');
                const id = tr.dataset.id;
                const nome = tr.dataset.nome;
                abrirModalEdicao(id, nome);
            });
        });
    }

    // 2. ADICIONAR COLABORADOR
    async function adicionarColaborador(e) {
        // ... (código existente, não muda)
        e.preventDefault();
        formColaboradorStatus.textContent = '';
        if (!adminToken) return;
        const dados = {
            nome: document.getElementById('novo-nome').value,
            cpf: document.getElementById('novo-cpf').value,
            matricula: document.getElementById('novo-matricula').value
        };
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dados)
            });
            if (!resposta.ok) {
                const erro = await resposta.json();
                throw new Error(erro.message || 'Erro ao criar colaborador.');
            }
            formColaboradorContainer.style.display = 'none';
            formNovoColaborador.reset();
            carregarColaboradores();
        } catch (err) {
            formColaboradorStatus.textContent = err.message;
        }
    }

    // 3. LISTAR ÁUDIOS
    async function carregarAudios() {
        // ... (código existente, não muda)
        if (!adminToken) return;
        listaAudiosContainer.innerHTML = '<p>A carregar áudios pendentes...</p>';
        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/audios`, {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${adminToken}` }
            });
            if (!resposta.ok) throw new Error('Não foi possível carregar os áudios.');
            const audios = await resposta.json();
            desenharTabelaAudios(audios);
        } catch (err) {
            listaAudiosContainer.innerHTML = `<p class="text-danger">${err.message}</p>`;
        }
    }

    function desenharTabelaAudios(audios) {
        // ... (código existente, não muda)
        if (audios.length === 0) {
            listaAudiosContainer.innerHTML = '<p>Nenhum áudio pendente encontrado.</p>';
            return;
        }
        let tabelaHtml = `<table class="table table-hover"><thead>...</thead><tbody>`;
        tabelaHtml = `
            <table class="table table-hover">
                <thead>
                    <tr>
                        <th>Protocolo</th>
                        <th>Data</th>
                        <th>Colaborador</th>
                        <th>CPF</th>
                        <th>Áudio</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
        `;
        for (const audio of audios) {
            const status = audio.resolvido 
                ? '<span class="badge bg-secondary">Resolvido</span>' 
                : '<span class="badge bg-danger">Pendente</span>';
            const audioUrl = `${API_BASE_URL}/${audio.caminhoArquivo}`;
            tabelaHtml += `
                <tr>
                    <td>${audio.id}</td>
                    <td>${new Date(audio.dataCriacao).toLocaleString('pt-BR')}</td>
                    <td>${audio.nomeColaborador}</td>
                    <td>${audio.cpfColaborador}</td>
                    <td><audio controls src="${audioUrl}" preload="none"></audio></td>
                    <td>${status}</td>
                </tr>
            `;
        }
        tabelaHtml += '</tbody></table>';
        listaAudiosContainer.innerHTML = tabelaHtml;
    }

    // --- ** NOVAS FUNÇÕES DE EDIÇÃO (v-nova) ** ---

    function abrirModalEdicao(id, nome) {
        // 1. Guarda o ID do colaborador que estamos a editar
        colaboradorEditandoId = id;
        
        // 2. Atualiza o título do modal
        editarModalTitulo.textContent = `A gerir dados de: ${nome} (ID: ${id})`;
        
        // 3. Limpa formulários e status antigos
        formEditarBancoHoras.reset();
        statusBancoHoras.textContent = '';
        formEditarFerias.reset();
        statusFerias.textContent = '';
        formEnviarHolerite.reset();
        statusHolerite.textContent = '';
        
        // 4. Mostra o modal
        bsModalEditar.show();
    }
    
    // Liga o formulário de Banco de Horas
    async function handleSalvarBancoHoras(e) {
        e.preventDefault();
        if (!adminToken || !colaboradorEditandoId) return;
        statusBancoHoras.textContent = 'A salvar...';

        const dados = {
            horasAcumuladas: parseFloat(document.getElementById('editar-horas').value),
            textoParaFala: document.getElementById('editar-horas-fala').value
        };

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${colaboradorEditandoId}/bancohoras`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dados)
            });
            if (!resposta.ok) throw new Error('Falha ao salvar.');
            statusBancoHoras.textContent = 'Salvo com sucesso!';
            statusBancoHoras.className = 'text-success mt-2';
        } catch (err) {
            statusBancoHoras.textContent = `Erro: ${err.message}`;
            statusBancoHoras.className = 'text-danger mt-2';
        }
    }

    // Liga o formulário de Férias
    async function handleSalvarFerias(e) {
        e.preventDefault();
        if (!adminToken || !colaboradorEditandoId) return;
        statusFerias.textContent = 'A salvar...';

        const dados = {
            dataInicio: document.getElementById('editar-ferias-inicio').value || null,
            dataFim: document.getElementById('editar-ferias-fim').value || null,
            diasDeSaldo: parseInt(document.getElementById('editar-ferias-saldo').value),
            textoParaFala: document.getElementById('editar-ferias-fala').value
        };

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${colaboradorEditandoId}/ferias`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(dados)
            });
            if (!resposta.ok) throw new Error('Falha ao salvar.');
            statusFerias.textContent = 'Salvo com sucesso!';
            statusFerias.className = 'text-success mt-2';
        } catch (err) {
            statusFerias.textContent = `Erro: ${err.message}`;
            statusFerias.className = 'text-danger mt-2';
        }
    }

    // Liga o formulário de Holerite (com FormData)
    async function handleEnviarHolerite(e) {
        e.preventDefault();
        if (!adminToken || !colaboradorEditandoId) return;
        statusHolerite.textContent = 'A enviar...';

        const pdfFile = document.getElementById('holerite-pdf').files[0];
        if (!pdfFile) {
            statusHolerite.textContent = 'Por favor, selecione um ficheiro PDF.';
            return;
        }

        // Usamos FormData para enviar ficheiros + dados
        const formData = new FormData();
        formData.append('MesAno', document.getElementById('holerite-mes-ano').value);
        formData.append('ValorLiquido', parseFloat(document.getElementById('holerite-valor').value));
        formData.append('TextoParaFala', document.getElementById('holerite-fala').value);
        formData.append('pdfFile', pdfFile, pdfFile.name);

        try {
            const resposta = await fetch(`${API_BASE_URL}/api/admin/colaboradores/${colaboradorEditandoId}/holerite`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${adminToken}`
                    // NÃO defina 'Content-Type', o FormData faz isso
                },
                body: formData
            });
            if (!resposta.ok) throw new Error('Falha ao enviar.');
            statusHolerite.textContent = 'Holerite enviado com sucesso!';
            statusHolerite.className = 'text-success mt-2';
        } catch (err) {
            statusHolerite.textContent = `Erro: ${err.message}`;
            statusHolerite.className = 'text-danger mt-2';
        }
    }

    // --- Registro de Eventos ---
    loginFormAdmin.addEventListener('submit', fazerLoginAdmin);
    btnAdminSair.addEventListener('click', fazerLogoutAdmin);

    // Eventos dos botões do painel
    btnMostrarForm.addEventListener('click', () => formColaboradorContainer.style.display = 'block');
    btnCancelarForm.addEventListener('click', () => {
        formColaboradorContainer.style.display = 'none';
        formNovoColaborador.reset();
        formColaboradorStatus.textContent = '';
    });
    formNovoColaborador.addEventListener('submit', adicionarColaborador);

    // Eventos de Navegação
    navColaboradores.addEventListener('click', (e) => {
        e.preventDefault();
        mostrarPainelColaboradores();
    });
    navAudios.addEventListener('click', (e) => {
        e.preventDefault();
        mostrarPainelAudios();
    });

    // Eventos do Modal de Edição (NOVOS)
    formEditarBancoHoras.addEventListener('submit', handleSalvarBancoHoras);
    formEditarFerias.addEventListener('submit', handleSalvarFerias);
    formEnviarHolerite.addEventListener('submit', handleEnviarHolerite);
    
    // --- Inicialização ---
    const tokenSalvo = sessionStorage.getItem('admin_token');
    if (tokenSalvo) {
        adminToken = tokenSalvo;
        mostrarTela(telaDashboardAdmin); 
    } else {
        mostrarTela(telaLoginAdmin);
    }
});