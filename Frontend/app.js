
// Angular Application (YANDES)
var app = angular.module('yandesApp', []);

// Simple controller-driven SPA with tabs and map stub
app.controller('YandesController', function($scope, $http) {
    var vm = this;
    
    vm.activeTab = 'map';
    vm.authMode = 'login'; // Default to login mode
    vm.alertMessage = '';
    vm.alertClass = '';
    vm.showHeat = false;
    vm.showWind = false;
    vm.points = 0;
    vm.map = null;
    vm.userMarker = null;
    vm.auth = { token: '', username: '' };
    vm.loginForm = { email: '', password: '' };
    vm.registerForm = { firstName: '', lastName: '', email: '', password: '' };
    vm.editMode = false;
    vm.passwordMode = false;
    vm.editForm = { firstName: '', lastName: '', email: '' };
    vm.passwordForm = { currentPassword: '', newPassword: '' };

    vm.setTab = function(tab) {
        vm.activeTab = tab;
        if (tab === 'map') {
            setTimeout(vm.ensureMap, 0);
        } else if (tab === 'game') {
            vm.loadGameState();
        } else if (tab === 'profile') {
            vm.loadProfile();
        }
    };

    vm.setAuthMode = function(mode) {
        vm.authMode = mode;
        // Clear forms when switching modes
        if (mode === 'login') {
            vm.loginForm = { email: '', password: '' };
        } else {
            vm.registerForm = { firstName: '', lastName: '', email: '', password: '' };
        }
    };

    vm.showAlert = function(message, type) {
        vm.alertMessage = message;
        vm.alertClass = 'alert-' + type;
        setTimeout(function() {
            vm.clearAlert();
            $scope.$apply();
        }, 4000);
    };

    vm.clearAlert = function() {
        vm.alertMessage = '';
        vm.alertClass = '';
    };

    vm.ensureMap = function() {
        if (vm.map || !window.L) return;
        var mapEl = document.getElementById('map');
        if (!mapEl) return;
        vm.map = L.map('map').setView([39.0, 35.0], 6);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap katkıda bulunanlar'
        }).addTo(vm.map);

        // Load recent hotspots from backend
        fetch('http://localhost:5180/api/fires/recent')
            .then(function(r) { return r.json(); })
            .then(function(payload) {
                if (!payload.success || !Array.isArray(payload.data)) return;
                if (payload.data.length === 0) {
                    vm.showAlert('Hotspot bulunamadı', 'info');
            return;
        }
                payload.data.forEach(function(f) {
                    var title = 'Hotspot (' + Math.round((f.confidence || 0) * 100) + '%)';
                    L.marker([f.latitude, f.longitude], { title: title }).addTo(vm.map)
                        .bindPopup('<b>' + title + '</b><br/>' + new Date(f.detectedAtUtc).toLocaleString());
                });
                try { console.debug('Recent hotspots loaded:', payload.data.length); } catch (e) {}
            })
            .catch(function() { /* ignore */ });
    };

    vm.locateMe = function() {
        if (!navigator.geolocation) {
            vm.showAlert('Geolocation desteklenmiyor', 'danger');
            return;
        }
        navigator.geolocation.getCurrentPosition(function(pos) {
            var lat = pos.coords.latitude;
            var lng = pos.coords.longitude;
            if (vm.map) {
                vm.map.setView([lat, lng], 12);
                if (vm.userMarker) {
                    vm.map.removeLayer(vm.userMarker);
                }
                vm.userMarker = L.marker([lat, lng], { title: 'Konumum' }).addTo(vm.map)
                    .bindPopup('Konumunuz');
                // Load nearby hotspots
                fetch('http://localhost:5180/api/fires/nearby?lat=' + lat + '&lng=' + lng + '&radiusKm=50')
                    .then(function(r) { return r.json(); })
                    .then(function(payload) {
                        if (!payload.success || !Array.isArray(payload.data)) return;
                        payload.data.forEach(function(f) {
                            var title = 'Yakın Hotspot (' + Math.round((f.confidence || 0) * 100) + '%)';
                            L.marker([f.latitude, f.longitude], { title: title }).addTo(vm.map)
                                .bindPopup('<b>' + title + '</b><br/>' + new Date(f.detectedAtUtc).toLocaleString());
                        });
                    })
                    .catch(function() { /* ignore */ });
            }
            $scope.$apply();
        }, function(err) {
            vm.showAlert('Konum alınamadı: ' + err.message, 'danger');
            $scope.$apply();
            });
    };
    
    // Initialize
    setTimeout(vm.ensureMap, 0);
    try {
        var saved = JSON.parse(localStorage.getItem('yandes_auth') || 'null');
        if (saved && saved.token) {
            vm.auth = saved;
            vm.setTab('map');
        }
    } catch (e) {}

    // Auth
    vm.register = function() {
        
        // Frontend validasyonu
        if (!vm.registerForm.firstName || !vm.registerForm.firstName.trim()) {
            vm.showAlert('Ad alanı zorunludur', 'danger');
            return;
        }
        if (!vm.registerForm.lastName || !vm.registerForm.lastName.trim()) {
            vm.showAlert('Soyad alanı zorunludur', 'danger');
            return;
        }
        if (!vm.registerForm.email || !vm.registerForm.email.trim()) {
            vm.showAlert('Email alanı zorunludur', 'danger');
            return;
        }
        if (!vm.registerForm.password || !vm.registerForm.password.trim()) {
            vm.showAlert('Şifre alanı zorunludur', 'danger');
            return;
        }
        if (vm.registerForm.password.length < 6) {
            vm.showAlert('Şifre en az 6 karakter olmalıdır', 'danger');
            return;
        }
        
        // Email format kontrolü
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(vm.registerForm.email)) {
            vm.showAlert('Geçerli bir email adresi giriniz', 'danger');
            return;
        }
        
        // Register data
        var registerData = {
            firstName: vm.registerForm.firstName,
            lastName: vm.registerForm.lastName,
            email: vm.registerForm.email,
            password: vm.registerForm.password
        };
        

        
        try {
            $http.post('http://localhost:5180/api/auth/register', registerData).then(function(r){
                if (r.data && r.data.success) {
                    vm.auth.token = r.data.data.token; vm.auth.username = r.data.data.username;
                    vm.showAlert('Kayıt başarılı', 'success');
                    vm.setTab('map');
                    try { localStorage.setItem('yandes_auth', JSON.stringify(vm.auth)); } catch (e) {}
                } else {
                    vm.showAlert(r.data && r.data.message ? r.data.message : 'Kayıt başarısız', 'danger');
                }
            }, function(err){
                vm.showAlert((err.data && err.data.message) ? err.data.message : 'Kayıt hatası', 'danger'); 
            });
        } catch (e) {
            vm.showAlert('Bağlantı hatası: ' + e.message, 'danger');
        }
    };
    vm.login = function() {
        
        // Frontend validasyonu
        if (!vm.loginForm.email || !vm.loginForm.email.trim()) {
            vm.showAlert('Email alanı zorunludur', 'danger');
            return;
        }
        if (!vm.loginForm.password || !vm.loginForm.password.trim()) {
            vm.showAlert('Şifre zorunludur', 'danger');
            return;
        }
        if (vm.loginForm.password.length < 6) {
            vm.showAlert('Şifre en az 6 karakter olmalıdır', 'danger');
            return;
        }
        
        // Email format kontrolü
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(vm.loginForm.email)) {
            vm.showAlert('Geçerli bir email adresi giriniz', 'danger');
            return;
        }
        
        var loginData = { email: vm.loginForm.email, password: vm.loginForm.password };
        
        try {
            $http.post('http://localhost:5180/api/auth/login', loginData).then(function(r){
                if (r.data && r.data.success) {
                    vm.auth.token = r.data.data.token; vm.auth.username = r.data.data.username;
                    vm.showAlert('Giriş başarılı', 'success');
                    vm.setTab('map');
                    try { localStorage.setItem('yandes_auth', JSON.stringify(vm.auth)); } catch (e) {}
                } else {
                    vm.showAlert(r.data.message || 'Giriş başarısız', 'danger');
                }
            }, function(err){
                vm.showAlert(err.data?.message || 'Giriş hatası', 'danger'); 
            });
        } catch (e) {
            vm.showAlert('Bağlantı hatası: ' + e.message, 'danger');
        }
    };
    vm.logout = function() { vm.auth = { token: '', username: '' }; try { localStorage.removeItem('yandes_auth'); } catch (e) {} };

    // Game/Profile
    vm.loadGameState = function() {
        if (!vm.auth.token) return;
        $http.get('http://localhost:5180/api/game/state', { headers: { Authorization: 'Bearer ' + vm.auth.token }})
            .then(function(r){ if (r.data && r.data.success) { vm.game = r.data.data; } });
    };
    vm.completeTask = function(key) {
        if (!vm.auth.token) return;
        $http.post('http://localhost:5180/api/game/complete', { key: key }, { headers: { Authorization: 'Bearer ' + vm.auth.token }})
            .then(function(r){ if (r.data && r.data.success) { vm.points = r.data.data.points; vm.loadGameState(); vm.showAlert(r.data.message, 'success'); } });
    };
    vm.loadProfile = function() {
        if (!vm.auth.token) return;
        $http.get('http://localhost:5180/api/profile', { headers: { Authorization: 'Bearer ' + vm.auth.token }})
            .then(function(r){ 
                if (r.data && r.data.success) { 
                    vm.profile = r.data.data;
                    // Profil bilgilerini edit formuna yükle
                    vm.editForm.firstName = vm.profile.firstName || '';
                    vm.editForm.lastName = vm.profile.lastName || '';
                    vm.editForm.email = vm.profile.email || '';
                } 
            });
    };

    // Profil düzenleme fonksiyonları
    vm.toggleEditMode = function() {
        vm.editMode = !vm.editMode;
        vm.passwordMode = false; // Şifre modunu kapat
        if (vm.editMode && vm.profile) {
            vm.editForm.firstName = vm.profile.firstName || '';
            vm.editForm.lastName = vm.profile.lastName || '';
            vm.editForm.email = vm.profile.email || '';
        }
    };

    vm.togglePasswordMode = function() {
        vm.passwordMode = !vm.passwordMode;
        vm.editMode = false; // Edit modunu kapat
        if (vm.passwordMode) {
            vm.passwordForm = { currentPassword: '', newPassword: '' };
        }
    };

    vm.updateProfile = function() {
        if (!vm.auth.token) {
            vm.showAlert('Giriş yapmanız gerekiyor', 'danger');
            return;
        }
        
        // Validasyon
        if (!vm.editForm.firstName || vm.editForm.firstName.trim() === '') {
            vm.showAlert('Ad zorunludur', 'danger');
            return;
        }
        if (!vm.editForm.lastName || vm.editForm.lastName.trim() === '') {
            vm.showAlert('Soyad zorunludur', 'danger');
            return;
        }
        if (!vm.editForm.email || vm.editForm.email.trim() === '') {
            vm.showAlert('Email zorunludur', 'danger');
            return;
        }
        
        // Email format kontrolü
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(vm.editForm.email)) {
            vm.showAlert('Geçerli bir email adresi giriniz', 'danger');
            return;
        }
        
        var updateData = {
            firstName: vm.editForm.firstName,
            lastName: vm.editForm.lastName,
            email: vm.editForm.email
        };

        $http.put('http://localhost:5180/api/profile', updateData, { 
            headers: { Authorization: 'Bearer ' + vm.auth.token } 
        }).then(function(r){
            if (r.data && r.data.success) {
                vm.showAlert('Profil başarıyla güncellendi', 'success');
                vm.editMode = false;
                
                // Email değiştiyse token'ı güncelle
                if (r.data.data && r.data.data.token) {
                    vm.auth.token = r.data.data.token;
                    vm.auth.username = r.data.data.email;
                    try { localStorage.setItem('yandes_auth', JSON.stringify(vm.auth)); } catch (e) {}
                }
                
                // Profili yeniden yükle
                vm.loadProfile();
            } else {
                vm.showAlert(r.data.message || 'Profil güncellenemedi', 'danger');
            }
        }, function(err){
            vm.showAlert(err.data?.message || 'Profil güncellenirken hata oluştu', 'danger');
        });
    };

    vm.changePassword = function() {
        if (!vm.auth.token) {
            vm.showAlert('Giriş yapmanız gerekiyor', 'danger');
            return;
        }
        
        // Validasyon
        if (!vm.passwordForm.currentPassword || vm.passwordForm.currentPassword.trim() === '') {
            vm.showAlert('Mevcut şifre zorunludur', 'danger');
            return;
        }
        if (!vm.passwordForm.newPassword || vm.passwordForm.newPassword.trim() === '') {
            vm.showAlert('Yeni şifre zorunludur', 'danger');
            return;
        }
        if (vm.passwordForm.newPassword.length < 6) {
            vm.showAlert('Yeni şifre en az 6 karakter olmalıdır', 'danger');
            return;
        }
        
        var passwordData = {
            currentPassword: vm.passwordForm.currentPassword,
            newPassword: vm.passwordForm.newPassword
        };

        $http.put('http://localhost:5180/api/profile/password', passwordData, { 
            headers: { Authorization: 'Bearer ' + vm.auth.token } 
        }).then(function(r){
            if (r.data && r.data.success) {
                vm.showAlert('Şifre başarıyla değiştirildi', 'success');
                vm.passwordMode = false;
                vm.passwordForm = { currentPassword: '', newPassword: '' };
            } else {
                vm.showAlert(r.data.message || 'Şifre değiştirilemedi', 'danger');
            }
        }, function(err){
            vm.showAlert(err.data?.message || 'Şifre değiştirilirken hata oluştu', 'danger');
        });
    };

    vm.cancelEdit = function() {
        vm.editMode = false;
        vm.editForm = { firstName: '', lastName: '', email: '' };
    };

    vm.cancelPasswordChange = function() {
        vm.passwordMode = false;
        vm.passwordForm = { currentPassword: '', newPassword: '' };
    };
}); 