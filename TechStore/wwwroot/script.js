const apiUrl = '/api/products';
const grid = document.getElementById('products-grid');

async function loadProducts() {
    try {
        const response = await fetch(`${apiUrl}?pageNumber=1&pageSize=20`); 
        
        if (!response.ok) {
            throw new Error('Ошибка загрузки данных');
        }

        const data = await response.json();
        
        const products = data.items || data.products || data; 

        renderProducts(products);
    } catch (error) {
        console.error(error);
        grid.innerHTML = '<p style="color:red">Не удалось загрузить товары.</p>';
    }
}

function renderProducts(products) {
    const grid = document.getElementById('products-grid');
    grid.innerHTML = '';

    console.log("Пример товара:", products[0]);

    products.forEach(product => {
        const card = document.createElement('div');
        card.className = 'product-card';

        // 1. ПРОВЕРКА Если imageUrl == null ставим заглушку
        let imageSrc = product.imageUrl;
        if (imageSrc === null || imageSrc === "") {
            imageSrc = 'https://placehold.co/300x300/png?text=No+Image'; // заглушка
        }

        // Защита для названия
        const name = product.name || 'Товар без названия';
        const price = product.price || 0;


    card.innerHTML = `
            <img src="${imageSrc}" alt="${name}" class="product-image">
            <h3 class="product-title">${name}</h3>
            <div class="product-price">${price} ₴</div>
            
            <button class="btn-buy" onclick="buyItem(${product.id || product.Id})">Купить</button>

            <hr style="margin: 10px 0; border: 0; border-top: 1px solid #eee;">
            
            <div style="display:flex; gap:5px;">
                <input type="text" id="img-input-${product.id}" placeholder="Ссылка на фото" style="width: 100%; padding: 5px;">
                <button onclick="updateImage(${product.id})" style="cursor:pointer;">💾</button>
            </div>
        `;

        card.dataset.productJson = JSON.stringify(product);

        grid.appendChild(card);
    });
}

// Функция обновления картинки
async function updateImage(id)
{
    const input = document.getElementById(`img-input-${id}`);
    const newUrl = input.value;

    if (!newUrl) {
        alert("Введите ссылку!");
        return;
    }

    const card = input.closest('.product-card'); 
    const fullProductData = JSON.parse(card.dataset.productJson);

    const cleanDto = {
        Name: fullProductData.name || fullProductData.Name,
        Price: fullProductData.price || fullProductData.Price,
        Stock: fullProductData.stock || fullProductData.Stock,
        Description: fullProductData.description || fullProductData.Description || "Описание отсутствует",
        ImageUrl: newUrl,
        IsActive: (fullProductData.isActive !== undefined) ? fullProductData.isActive : true
    };

    console.log("Отправляем на сервер:", cleanDto);

    try{
        const response = await fetch(`/api/products/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${userToken}`
            },
            body: JSON.stringify(cleanDto)
        });

        if (response.ok) {
            alert("Картинка обновлена!");
            location.reload(); // Перезагружаем страницу, чтобы увидеть результат
        } else {
            const errorText = await response.text();
            console.error("Ошибка сервера:", errorText);
            alert(`Ошибка обновления: ${response.status} \nПосмотри консоль (F12)`);
        }

        } catch (e) {
        console.error(e);
        alert("Ошибка сети");
    }
    
}

function addToCart(id) {
    alert(`Товар ID ${id} добавлен в корзину (пока понарошку)`);
}

// Запуск при загрузке страницы
loadProducts();

// (ТЕСТ) Быстрая авторизация для покупки
// Глобальная переменная для токена
let userToken = "";

// 1. Функция входа (чтобы не делать форму регистрации)
// сделать поля input email/password
async function loginDemo() {
    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email: "Your email", 
                password: "Your password"      
            })
        });

        if (response.ok) {
            const data = await response.json();
            userToken = data.token; // Сохраняем токен в память
            alert("Вы вошли в систему! Теперь можно покупать и редактировать.");
            
            const authPanel = document.getElementById('auth-panel');
            if (authPanel) authPanel.innerHTML = '<span style="color: lightgreen;">Вы авторизованы ✅</span>';
        } else {
            alert("Ошибка входа. Проверьте логин/пароль в script.js");
        }
    } catch (e) {
        console.error(e);
        alert("Сервер недоступен");
    }
}

// 2. Функция покупки
async function buyItem(productId) {
    if (!userToken) {
        const doLogin = confirm("Для покупки нужно войти. Войти как тест-юзер?");
        if (doLogin) await loginDemo();
        else return;
    }

    if (!userToken) return; // Если так и не вошли

    try {
        const orderData = {
            items: [
                { productId: productId, quantity: 1 } // Покупаем 1 штуку
            ]
        };

        const response = await fetch('/api/orders', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${userToken}` // ВАЖНО! Шлем токен
            },
            body: JSON.stringify(orderData)
        });

        if (response.ok) {
            alert(`Ура! Товар ID ${productId} куплен! Склад обновлен.`);
            
            loadProducts();
        } else {
            const err = await response.text();
            alert("Ошибка покупки: " + err);
        }
    } catch (e) {
        console.error(e);
        alert("Ошибка сети");
    }
}