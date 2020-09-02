# Designer Differ

Git ile üretilen kodlardaki karmaşıklığı azaltıp, düzgün bir versiyon kıyaslaması sağlayan eklenti

## ⭐ Gelinen Aşama

- `Form.Designer.cs` dosyasına sağ tıklayın `Compare with History...` butonunu seçin
- Çıkan form üzerinden istenen branch ve commit hash ile eski sürümü ile kıyaslamayı yapılandırın
- Dosyanın bir önceki sürümü `%TEMP%` dizininde `~<filename>` adıyla oluşturulur
    - `%TEMP%` dizini işletim sistemi otomatik olarak temizlenen bir dizindir
- Oluşturulan geçici dosya aktif projeye dahil edilir ve windows form tarafından üretilen kodlar sıralanır, kaydedilir
- Seçilen dosya içeriğindeki windows form tarafından üretilen kodlar sıralanır, kaydedilmez
- Dosyalar Visual Studio 2019 Tools aracı olan DiffFiles ile karşılaştırılır
