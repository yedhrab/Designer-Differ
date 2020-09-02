## VSIX XAML Notları

### Sabit değer ekleme ve kullanma

- `platformUi:DialogWindow` xaml objesinin en üstteki tagını temsil eder (sizde farklıdır)

```xml
<platformUi:DialogWindow.Resources>
    <SolidColorBrush x:Key="brushWatermarkBackground" Color="White" />
</platformUi:DialogWindow.Resources>

<!--Title="Compare with history"-->
<Grid Background="{StaticResource brushWatermarkBackground}">
```

### Değiştirilemez text box 

- TextBox özelliklerine `IsReadOnly="True"` ekleyin
- `IsDefault="True"` otomatik olarak odaklanılan buton
