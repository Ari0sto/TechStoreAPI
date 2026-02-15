const apiUrl = '/api/products';
let userToken = ""; // Глобальный токен

// Глобал. переменные для пагинации
let curretPage = 1;
const pageSize = 8;
let totalPages = 1;

// Корзина
let cart = [];

// 1. Загрузка корзины при старте
function loadCart() {
    const savedCart = localStorage.getItem('techStoreCart');
    if (savedCart) {
        cart = JSON.parse(savedCart);
        updateCartCount();
    }
}

// 2. Добавление товара
function addToCart(product) {
    // Проверка есть ли уже такой товар
    const existingItem = cart.find(item => item.id === product.id);
    
    if (existingItem) {
        existingItem.quantity++;
    } else {
        cart.push({
            id: product.id,
            name: product.name,
            price: product.price,
            imageUrl: product.imageUrl,
            quantity: 1
        });
    }
    saveCart();
    alert(`Товар "${product.name}" добавлен в корзину!`);
}

// 3. Сохранение корзины
function saveCart() {
    localStorage.setItem('techStoreCart', JSON.stringify(cart));
    updateCartCount();
}

// 4. Обновление счетчика корзины
function updateCartCount() {
    const count = cart.reduce((sum, item) => sum + item.quantity, 0);
    document.getElementById('cart-count').innerText = count;
}

// ЗАГРУЗКА ТОВАРОВ 
async function loadProducts(page = 1) {
    const grid = document.getElementById('products-grid');
    const pageInfo = document.getElementById('page-info');
    const btnPrev = document.getElementById('btn-prev');
    const btnNext = document.getElementById('btn-next');

    grid.innerHTML = '<p>Загрузка...</p>';

    try {
        const response = await fetch(`${apiUrl}?page=${page}&size=${pageSize}`);
        if (!response.ok) throw new Error('Ошибка загрузки');
        
        const data = await response.json();

        currentPage = data.currentPage;
        totalPages = data.totalPages;

        renderProducts(data.items);
        pageInfo.innerText = `Стр. ${currentPage} из ${totalPages}`;
        btnPrev.disabled = (currentPage === 1);
        btnNext.disabled = (currentPage >= totalPages);

    } catch (error) {
        console.error(error);
        grid.innerHTML = '<p style="color:red">Не удалось загрузить товары.</p>';
    }
}

// Переключение страниц
function changePage(direction){
    const newPage = currentPage + direction;

    if (newPage > 0 && newPage <= totalPages) {
        loadProducts(newPage);
        // Плавный скролл наверх
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function renderProducts(products) {
    const grid = document.getElementById('products-grid');
    grid.innerHTML = '';

    products.forEach(product => {
        const card = document.createElement('div');
        card.className = 'product-card';

        // Формируем путь к картинке
        let imageSrc = product.imageUrl;
        if (!imageSrc) {
            imageSrc = 'https://placehold.co/300x300/png?text=No+Image';
        }

        const name = product.name || 'Товар';
        const price = product.price || 0;

        const productData = JSON.stringify(product).replace(/"/g, '&quot;');

        card.innerHTML = `
            <img src="${imageSrc}" alt="${name}" class="product-image">
            <h3 class="product-title">${name}</h3>
            <div class="product-price">${price} ₴</div>

            <button class="btn-buy" onclick='addToCart(${productData})'>В корзину</button>
        `;
        grid.appendChild(card);
    });
}

// ДОБАВЛЕНИЕ ТОВАРА
document.getElementById('add-product-form').addEventListener('submit', async function(e) {
    e.preventDefault();

    if (!userToken) {
        alert("Сначала войдите как админ!");
        return;
    }

    // FormData автоматически собирает все поля, включая <input type="file">
    const formData = new FormData(this);

    try {
        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${userToken}` 
            },
            body: formData
        });

        if (response.ok) {
            alert("Товар успешно создан!");
            this.reset(); // Очистить форму
            loadProducts(); // Обновить список
        } else {
            const err = await response.text();
            alert("Ошибка сервера: " + err);
        }
    } catch (error) {
        console.error(error);
        alert("Ошибка сети");
    }
});

// АВТОРИЗАЦИЯ

// 1. Открытие/Закрытие модального окна
function openLoginModal() {
    document.getElementById('login-modal').style.display = 'block';
}

function closeLoginModal() {
    document.getElementById('login-modal').style.display = 'none';
    document.getElementById('login-error').style.display = 'none';
}

// Закрыть при клике вне окна
window.onclick = function(event) {
    const modal = document.getElementById('login-modal');
    if (event.target == modal) closeLoginModal();
}

// 2. Обработка формы входа
document.getElementById('login-form').addEventListener('submit', async function(e) {
    e.preventDefault();

    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;
    const errorMsg = document.getElementById('login-error');

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
    });

    if (response.ok) {
        const data = await response.json();

        // Сохранение токена
        localStorage.setItem('techStoreToken', data.token);

        localStorage.setItem('userRole', data.role);

        alert("Успешный вход!");
        closeLoginModal();
        checkAuthStatus();

    }else{
        errorMsg.innerText = "Неверный логин или пароль";
        errorMsg.style.display = 'block';
    }
    } catch (err) {
        console.error(err);
        errorMsg.innerText = "Ошибка сервера";
        errorMsg.style.display = 'block';
    }
});

// 3. Проверка статуса авторизации при загрузке страницы
function checkAuthStatus() {
    const token = localStorage.getItem('techStoreToken');
    const authBtnContainer = document.getElementById('auth-status');
    const adminPanel = document.getElementById('admin-panel');

    // По умолчанию админка скрыта
    if (adminPanel) adminPanel.style.display = 'none';

    if (token) {
        // 1. Декодируем токен
        const decodedToken = parseJwt(token);
        console.log("Содержимое токена:", decodedToken);

        // 2. Ищем роль
        const userRole = decodedToken["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] 
                         || decodedToken["role"] 
                         || "User";

        // 3. Логика интерфейса
        authBtnContainer.innerHTML = `
            <span style="color: white; margin-right: 10px;">Привет, ${decodedToken.sub || decodedToken.email || "Гость"}!</span>
            <button class="btn-login" onclick="logout()">Выйти</button>
        `;

        // 4. Показываем админку ТОЛЬКО если роль Admin
        if (userRole === "Admin" && adminPanel) {
            adminPanel.style.display = 'block';
        }

        userToken = token;
    } else {
        // гость
        authBtnContainer.innerHTML = `
            <button class="btn-login" onclick="openLoginModal()">Войти</button>
        `;
        userToken = "";
    }
}

// 4. Выход
function logout() {
    localStorage.removeItem('techStoreToken');
    checkAuthStatus();
    alert("Вы вышли из системы");
}


// ПОКУПКА (Заглушка)
function buyItem(id) {
    alert(`Товар ID ${id} добавлен в корзину!`);
}

// Парс JWT для получения роли
function parseJwt(token) {
    try {
        var base64Url = token.split('.')[1];
        var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        var jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

// Старт
loadProducts();
checkAuthStatus();
loadCart();