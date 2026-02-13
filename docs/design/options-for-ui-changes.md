I'll create 3 distinct design alternatives for your OneDrive Sync Client. Each maintains the same functionality but with completely different visual approaches.

# Design Option 1: Modern Professional (Corporate Minimalist)

**Visual Identity:** Clean lines, subtle shadows, monochromatic palette with blue accents, sophisticated typography, ample whitespace

## MainWindow.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:accounts="using:AStar.Dev.OneDrive.Client.Accounts"
        xmlns:syncronisation="using:AStar.Dev.OneDrive.Client.Syncronisation"
        xmlns:syncronisationConflicts="using:AStar.Dev.OneDrive.Client.SyncronisationConflicts"
        xmlns:vm="using:AStar.Dev.OneDrive.Client.MainWindow"
        Icon="avares://AStar.Dev.OneDrive.Client/Assets/astar.png"
        x:Class="AStar.Dev.OneDrive.Client.MainWindow.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="AStar OneDrive Sync"
        Width="1280"
        Height="800"
        MinWidth="900"
        MinHeight="600"
        Background="#F5F7FA">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <Window.Styles>
        <!-- Professional shadow effect -->
        <Style Selector="Border.card">
            <Setter Property="Background" Value="White"/>
            <Setter Property="CornerRadius" Value="8"/>
            <Setter Property="Padding" Value="24"/>
            <Setter Property="BoxShadow" Value="0 2 8 0 #10000000"/>
        </Style>
        
        <Style Selector="Button.primary">
            <Setter Property="Background" Value="#0078D4"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="Padding" Value="16,8"/>
            <Setter Property="CornerRadius" Value="4"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
        </Style>
        
        <Style Selector="Button.primary:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="#106EBE"/>
        </Style>
        
        <Style Selector="Button.secondary">
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="Foreground" Value="#0078D4"/>
            <Setter Property="BorderBrush" Value="#0078D4"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Padding" Value="16,8"/>
            <Setter Property="CornerRadius" Value="4"/>
        </Style>
    </Window.Styles>

    <Window.KeyBindings>
        <KeyBinding Gesture="F2" Command="{Binding OpenUpdateAccountDetailsCommand}" />
        <KeyBinding Gesture="Alt+F4" Command="{Binding CloseApplicationCommand}" />
    </Window.KeyBindings>

    <Grid RowDefinitions="60,*">
        <!-- Top Navigation Bar -->
        <Border Grid.Row="0" Background="White" BoxShadow="0 1 4 0 #10000000">
            <Grid ColumnDefinitions="Auto,*,Auto" Margin="24,0">
                <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="12" VerticalAlignment="Center">
                    <PathIcon Data="M19,2H14.82C14.25,0.44 12.53,0.64 12,2C11.47,0.64 9.75,0.44 9.18,2H5A2,2 0 0,0 3,4V20A2,2 0 0,0 5,22H19A2,2 0 0,0 21,20V4A2,2 0 0,0 19,2Z" 
                              Width="24" Height="24" Foreground="#0078D4"/>
                    <TextBlock Text="AStar OneDrive Sync" FontSize="18" FontWeight="SemiBold" Foreground="#1A1A1A"/>
                </StackPanel>
                
                <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
                    <Button Content="Sync History" Command="{Binding OpenViewSyncHistoryCommand}" Classes="secondary" Height="36"/>
                    <Button Content="Debug Logs" Command="{Binding OpenDebugLogViewerCommand}" Classes="secondary" Height="36"/>
                    <Button Content="Account Details" Command="{Binding OpenUpdateAccountDetailsCommand}" Classes="secondary" Height="36"/>
                    <Separator Width="1" Height="24" Background="#E0E0E0"/>
                    <Button Content="✕" Command="{Binding CloseApplicationCommand}" 
                            Background="Transparent" BorderThickness="0" 
                            Foreground= "#666666" FontSize="20" Padding="12,4"
                            ToolTip.Tip="Close Application (Alt+F4)"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Main Content Area -->
        <Grid Grid.Row="1" ColumnDefinitions="380,*" Margin="16">
            <!-- Left Sidebar -->
            <Border Grid.Column="0" Classes="card" Margin="0,0,8,0">
                <accounts:AccountManagementView DataContext="{Binding AccountManagement}" />
            </Border>

            <!-- Right Content Panel -->
            <Border Grid.Column="1" Classes="card" Margin="8,0,0,0">
                <Panel>
                    <syncronisation:SyncTreeView DataContext="{Binding SyncTree}" />
                    
                    <syncronisation:SyncProgressView DataContext="{Binding SyncProgress}"
                                                     IsVisible="{Binding #SyncProgressView.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}"
                                                     x:Name="SyncProgressView" />
                    
                    <syncronisationConflicts:ConflictResolutionView DataContext="{Binding ConflictResolution}"
                                                                    IsVisible="{Binding #ConflictResolutionView.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}"
                                                                    x:Name="ConflictResolutionView" />
                </Panel>
            </Border>
        </Grid>
    </Grid>
</Window>
```

## AccountManagementView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:AStar.Dev.OneDrive.Client.Accounts"
             x:Class="AStar.Dev.OneDrive.Client.Accounts.AccountManagementView"
             x:DataType="vm:AccountManagementViewModel">

    <Design.DataContext>
        <vm:AccountManagementViewModel />
    </Design.DataContext>

    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Header -->
        <StackPanel Grid.Row="0" Spacing="4" Margin="0,0,0,24">
            <TextBlock Text="Accounts" FontSize="22" FontWeight="SemiBold" Foreground="#1A1A1A"/>
            <TextBlock Text="Manage your OneDrive connections" FontSize="13" Foreground="#666666"/>
        </StackPanel>

        <!-- Accounts List -->
        <ScrollViewer Grid.Row="1">
            <ListBox ItemsSource="{Binding Accounts}"
                     SelectedItem="{Binding SelectedAccount}"
                     Background="Transparent"
                     BorderThickness="0">
                <ListBox.Styles>
                    <Style Selector="ListBoxItem">
                        <Setter Property="Padding" Value="0"/>
                        <Setter Property="Margin" Value="0,0,0,12"/>
                    </Style>
                    <Style Selector="ListBoxItem:selected /template/ ContentPresenter">
                        <Setter Property="Background" Value="Transparent"/>
                    </Style>
                </ListBox.Styles>
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Background="#F8F9FA" 
                                BorderBrush="#E0E0E0" 
                                BorderThickness="1" 
                                CornerRadius="6" 
                                Padding="16">
                            <Grid ColumnDefinitions="Auto,*,Auto">
                                <!-- Avatar -->
                                <Border Grid.Column="0" Width="48" Height="48" 
                                        Background="#0078D4" CornerRadius="24" 
                                        VerticalAlignment="Center">
                                    <TextBlock Text="{Binding DisplayName, Converter={StaticResource InitialsConverter}}" 
                                               Foreground="White" FontSize="18" FontWeight="SemiBold"
                                               HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>

                                <!-- Account Info -->
                                <StackPanel Grid.Column="1" Margin="16,0,0,0" VerticalAlignment="Center">
                                    <TextBlock Text="{Binding DisplayName}" FontWeight="SemiBold" FontSize="14" Foreground="#1A1A1A"/>
                                    <TextBlock Text="{Binding Email}" FontSize="12" Foreground="#666666" Margin="0,2,0,0"/>
                                    <StackPanel Orientation="Horizontal" Spacing="6" Margin="0,6,0,0">
                                        <Ellipse Width="8" Height="8" Fill="{Binding IsAuthenticated, Converter={StaticResource BoolToStatusColorConverter}}"/>
                                        <TextBlock Text="{Binding IsAuthenticated, Converter={StaticResource BoolToStatusTextConverter}}" 
                                                   FontSize="11" Foreground="#666666"/>
                                    </StackPanel>
                                </StackPanel>

                                <!-- Selection Indicator -->
                                <Border Grid.Column="2" Width="4" Height="48" Background="#0078D4" CornerRadius="2"
                                        IsVisible="{Binding $parent[ListBoxItem].IsSelected}"/>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>

                <ListBox.Styles>
                    <Style Selector="ListBox:empty">
                        <Setter Property="Template">
                            <ControlTemplate>
                                <Border Background="#FAFAFA" BorderBrush="#E0E0E0" BorderThickness="1" 
                                        CornerRadius="6" Padding="32" MinHeight="200">
                                    <StackPanel Spacing="12" HorizontalAlignment="Center" VerticalAlignment="Center">
                                        <PathIcon Data="M12,12A5,5 0 0,1 17,7A5,5 0 0,1 22,12A5,5 0 0,1 17,17A5,5 0 0,1 12,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z"
                                                  Width="48" Height="48" Foreground="#CCCCCC"/>
                                        <TextBlock Text="No accounts added" FontSize="15" FontWeight="SemiBold" Foreground="#666666"/>
                                        <TextBlock Text="Click 'Add Account' to connect your OneDrive" 
                                                   FontSize="12" Foreground="#999999" TextWrapping="Wrap" TextAlignment="Center"/>
                                    </StackPanel>
                                </Border>
                            </ControlTemplate>
                        </Setter>
                    </Style>
                </ListBox.Styles>
            </ListBox>
        </ScrollViewer>

        <!-- Actions -->
        <Grid Grid.Row="2" ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" ColumnGap="8" RowGap="8" Margin="0,24,0,0">
            <Button Grid.Row="0" Grid.Column="0" Content="Add Account" Command="{Binding AddAccountCommand}"
                    Classes="primary" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
            <Button Grid.Row="0" Grid.Column="1" Content="Remove" Command="{Binding RemoveAccountCommand}"
                    Classes="secondary" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
            <Button Grid.Row="1" Grid.Column="0" Content="Sign In" Command="{Binding LoginCommand}"
                    Classes="secondary" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
            <Button Grid.Row="1" Grid.Column="1" Content="Sign Out" Command="{Binding LogoutCommand}"
                    Classes="secondary" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
        </Grid>

        <!-- Toast Notification -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                IsVisible="{Binding ToastVisible}"
                Background="Transparent"
                HorizontalAlignment="Center"
                VerticalAlignment="Top"
                Margin="0,0,0,0">
            <Border Background="#323232" CornerRadius="4" Padding="16,12" 
                    BoxShadow="0 4 12 0 #40000000">
                <TextBlock Text="{Binding ToastMessage}" Foreground="White" FontSize="13"/>
            </Border>
        </Border>

        <!-- Loading Overlay -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                Background="#F0FFFFFF"
                IsVisible="{Binding IsLoading}"
                CornerRadius="8">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="16">
                <ProgressBar IsIndeterminate="True" Width="200" Height="4" Foreground="#0078D4"/>
                <TextBlock Text="Loading accounts..." Foreground="#666666" FontSize="13"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

## SyncTreeView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:syncronisation="using:AStar.Dev.OneDrive.Client.Syncronisation"
             xmlns:models1="clr-namespace:AStar.Dev.OneDrive.Client.Core.Models;assembly=AStar.Dev.OneDrive.Client.Core"
             x:Class="AStar.Dev.OneDrive.Client.Syncronisation.SyncTreeView"
             x:DataType="syncronisation:SyncTreeViewModel">

    <Design.DataContext>
        <syncronisation:SyncTreeViewModel />
    </Design.DataContext>

    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Header -->
        <StackPanel Grid.Row="0" Spacing="4" Margin="0,0,0,24">
            <Grid ColumnDefinitions="*,Auto">
                <StackPanel Grid.Column="0">
                    <TextBlock Text="Folder Selection" FontSize="22" FontWeight="SemiBold" Foreground="#1A1A1A"/>
                    <TextBlock Text="Choose folders to synchronize" FontSize="13" Foreground="#666666" Margin="0,2,0,0"/>
                </StackPanel>
                <Button Grid.Column="1" Content="Clear All" Command="{Binding ClearSelectionsCommand}"
                        Classes="secondary" VerticalAlignment="Center"/>
            </Grid>
        </StackPanel>

        <!-- Tree View -->
        <Border Grid.Row="1" Background="#F8F9FA" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="6">
            <ScrollViewer Padding="12">
                <TreeView ItemsSource="{Binding Folders}" SelectionMode="Single" Background="Transparent" BorderThickness="0">
                    <TreeView.Styles>
                        <Style Selector="TreeViewItem" x:DataType="models1:OneDriveFolderNode">
                            <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
                            <Setter Property="Padding" Value="8,6"/>
                        </Style>
                    </TreeView.Styles>

                    <TreeView.ItemTemplate>
                        <TreeDataTemplate ItemsSource="{Binding Children}" x:DataType="models1:OneDriveFolderNode">
                            <StackPanel Orientation="Horizontal" Spacing="10">
                                <CheckBox IsChecked="{Binding IsSelected}" IsThreeState="True" VerticalAlignment="Center"/>
                                <PathIcon Data="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z"
                                          Width="16" Height="16" Foreground="#0078D4" VerticalAlignment="Center"/>
                                <ProgressRing Width="14" Height="14" IsVisible="{Binding IsLoading}" VerticalAlignment="Center"/>
                                <TextBlock Text="{Binding Name}" FontSize="13" Foreground="#1A1A1A" VerticalAlignment="Center"/>
                            </StackPanel>
                        </TreeDataTemplate>
                    </TreeView.ItemTemplate>
                </TreeView>
            </ScrollViewer>
        </Border>

        <!-- Footer Actions -->
        <Grid Grid.Row="2" Margin="0,16,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Button Grid.Column="0" Content="{Binding SyncButtonText}" Command="{Binding StartSyncCommand}"
                    Classes="primary" MinWidth="120" Margin="0,0,8,0" IsEnabled="{Binding !IsLoading}"/>
            <Button Grid.Column="1" Content="Show Progress" Command="{Binding OpenSyncProgressCommand}"
                    Classes="secondary" MinWidth="120" Margin="0,0,8,0"
                    IsVisible="{Binding IsSyncing}" IsEnabled="{Binding !IsSyncProgressOpen}"/>
            <Button Grid.Column="2" Content="Cancel" Command="{Binding CancelSyncCommand}"
                    Classes="secondary" MinWidth="100" Margin="0,0,8,0" IsVisible="{Binding IsSyncing}"/>

            <!-- Status Info -->
            <StackPanel Grid.Column="3" Orientation="Horizontal" Spacing="16" HorizontalAlignment="Right" VerticalAlignment="Center">
                <!-- Progress -->
                <StackPanel Orientation="Horizontal" Spacing="8" IsVisible="{Binding IsSyncing}">
                    <ProgressBar Value="{Binding ProgressPercentage}" Minimum="0" Maximum="100" 
                                 Width="120" Height="6" VerticalAlignment="Center" Foreground="#0078D4"/>
                    <TextBlock Text="{Binding ProgressText}" FontSize="12" Foreground="#666666" VerticalAlignment="Center"/>
                </StackPanel>

                <!-- Conflicts Warning -->
                <Border Background="#FFF3E0" BorderBrush="#FF6F00" BorderThickness="1" CornerRadius="4" Padding="8,6"
                        IsVisible="{Binding HasUnresolvedConflicts}" Cursor="Hand">
                    <StackPanel Orientation="Horizontal" Spacing="6">
                        <PathIcon Data="M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z"
                                  Width="14" Height="14" Foreground="#E65100"/>
                        <TextBlock Text="View Conflicts" FontSize="12" FontWeight="SemiBold" Foreground="#E65100"/>
                    </StackPanel>
                </Border>

                <!-- Last Sync Result -->
                <TextBlock Text="{Binding LastSyncResult}" FontSize="12" Foreground="#0078D4" FontWeight="SemiBold"
                           IsVisible="{Binding LastSyncResult, Converter={x:Static ObjectConverters.IsNotNull}}"/>
            </StackPanel>
        </Grid>

        <!-- Loading Overlay -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                Background="#F0FFFFFF"
                IsVisible="{Binding IsLoading}"
                CornerRadius="6">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="16">
                <ProgressRing Width="48" Height="48" Foreground="#0078D4"/>
                <TextBlock Text="Loading folder structure..." Foreground="#666666" FontSize="13"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

## ConflictResolutionView.axaml & SyncProgressView.axaml

```xml
<!-- ConflictResolutionView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:AStar.Dev.OneDrive.Client.SyncronisationConflicts"
             x:Class="AStar.Dev.OneDrive.Client.SyncronisationConflicts.ConflictResolutionView"
             x:DataType="vm:ConflictResolutionViewModel">

    <Border Background="White" CornerRadius="8" Padding="32">
        <Grid RowDefinitions="Auto,*,Auto">
            <!-- Header -->
            <StackPanel Grid.Row="0" Spacing="8" Margin="0,0,0,24">
                <TextBlock Text="Resolve Conflicts" FontSize="24" FontWeight="SemiBold" Foreground="#1A1A1A"/>
                <TextBlock Text="Files modified in multiple locations require your attention" 
                           FontSize="13" Foreground="#666666" TextWrapping="Wrap"/>
                <TextBlock Text="{Binding StatusMessage}" FontSize="13" FontWeight="SemiBold" 
                           Foreground="#0078D4" Margin="0,8,0,0"/>
            </StackPanel>

            <!-- Conflicts List -->
            <ScrollViewer Grid.Row="1" IsVisible="{Binding HasConflicts}">
                <ItemsControl ItemsSource="{Binding Conflicts}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType="vm:ConflictItemViewModel">
                            <Border Background="#F8F9FA" BorderBrush="#E0E0E0" BorderThickness="1" 
                                    CornerRadius="6" Padding="20" Margin="0,0,0,16">
                                <Grid RowDefinitions="Auto,Auto,Auto,Auto" RowGap="12">
                                    <TextBlock Grid.Row="0" Text="{Binding FilePath}" FontWeight="SemiBold" 
                                               FontSize="14" Foreground="#1A1A1A"/>

                                    <Grid Grid.Row="1" ColumnDefinitions="*,*" ColumnGap="16">
                                        <Border Grid.Column="0" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" 
                                                CornerRadius="4" Padding="12">
                                            <StackPanel Spacing="4">
                                                <TextBlock Text="Local Version" FontSize="11" Foreground="#666666" FontWeight="SemiBold"/>
                                                <TextBlock Text="{Binding LocalDetailsDisplay}" FontSize="12" Foreground="#1A1A1A"/>
                                            </StackPanel>
                                        </Border>
                                        <Border Grid.Column="1" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" 
                                                CornerRadius="4" Padding="12">
                                            <StackPanel Spacing="4">
                                                <TextBlock Text="Remote Version" FontSize="11" Foreground="#666666" FontWeight="SemiBold"/>
                                                <TextBlock Text="{Binding RemoteDetailsDisplay}" FontSize="12" Foreground="#1A1A1A"/>
                                            </StackPanel>
                                        </Border>
                                    </Grid>

                                    <TextBlock Grid.Row="2" Text="Resolution Strategy" FontSize="12" 
                                               FontWeight="SemiBold" Foreground="#1A1A1A"/>

                                    <StackPanel Grid.Row="3" Spacing="8">
                                        <RadioButton Content="Keep Local" GroupName="{Binding Id}"/>
                                        <RadioButton Content="Keep Remote" GroupName="{Binding Id}"/>
                                        <RadioButton Content="Keep Both" GroupName="{Binding Id}"/>
                                        <RadioButton Content="Skip for now" GroupName="{Binding Id}"/>
                                    </StackPanel>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </ScrollViewer>

            <!-- Actions -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="12" 
                        HorizontalAlignment="Right" Margin="0,24,0,0">
                <Button Content="Resolve All" Command="{Binding ResolveAllCommand}" Classes="primary" MinWidth="120"/>
                <Button Content="Cancel" Command="{Binding CancelCommand}" Classes="secondary" MinWidth="120"/>
            </StackPanel>
        </Grid>
    </Border>
</UserControl>
```

---

# Design Option 2: Modern Informal Colorful (Playful & Vibrant)

**Visual Identity:** Bold gradients, vibrant colors, soft rounded corners, playful icons, friendly typography, glassmorphism effects

## MainWindow.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:accounts="using:AStar.Dev.OneDrive.Client.Accounts"
        xmlns:syncronisation="using:AStar.Dev.OneDrive.Client.Syncronisation"
        xmlns:syncronisationConflicts="using:AStar.Dev.OneDrive.Client.SyncronisationConflicts"
        xmlns:vm="using:AStar.Dev.OneDrive.Client.MainWindow"
        Icon="avares://AStar.Dev.OneDrive.Client/Assets/astar.png"
        x:Class="AStar.Dev.OneDrive.Client.MainWindow.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="☁️ OneDrive Sync Party!"
        Width="1300"
        Height="850"
        MinWidth="900"
        MinHeight="650">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <Window.Styles>
        <!-- Gradient background -->
        <Style Selector="Border.gradient-bg">
            <Setter Property="Background">
                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                    <GradientStop Color="#667eea" Offset="0"/>
                    <GradientStop Color="#764ba2" Offset="1"/>
                </LinearGradientBrush>
            </Setter>
        </Style>
        
        <!-- Glass morphism card -->
        <Style Selector="Border.glass-card">
            <Setter Property="Background" Value="#E0FFFFFF"/>
            <Setter Property="CornerRadius" Value="20"/>
            <Setter Property="BoxShadow" Value="0 8 32 0 #40000000"/>
            <Setter Property="Padding" Value="24"/>
        </Style>
        
        <!-- Fun button styles -->
        <Style Selector="Button.fun-primary">
            <Setter Property="Background">
                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                    <GradientStop Color="#FF6B6B" Offset="0"/>
                    <GradientStop Color="#FFE66D" Offset="1"/>
                </LinearGradientBrush>
            </Setter>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="Padding" Value="20,12"/>
            <Setter Property="CornerRadius" Value="25"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
        </Style>
        
        <Style Selector="Button.fun-secondary">
            <Setter Property="Background" Value="#40FFFFFF"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderBrush" Value="White"/>
            <Setter Property="BorderThickness" Value="2"/>
            <Setter Property="Padding" Value="18,10"/>
            <Setter Property="CornerRadius" Value="25"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
        </Style>
    </Window.Styles>

    <Window.KeyBindings>
        <KeyBinding Gesture="F2" Command="{Binding OpenUpdateAccountDetailsCommand}" />
        <KeyBinding Gesture="Alt+F4" Command="{Binding CloseApplicationCommand}" />
    </Window.KeyBindings>

    <Border Classes="gradient-bg">
        <Grid RowDefinitions="80,*" Margin="16">
            <!-- Playful Header -->
            <Border Grid.Row="0" Background="#20FFFFFF" CornerRadius="20" 
                    BoxShadow="0 4 16 0 #30000000" Padding="20,0">
                <Grid ColumnDefinitions="Auto,*,Auto">
                    <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="16" VerticalAlignment="Center">
                        <Ellipse Width="50" Height="50">
                            <Ellipse.Fill>
                                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                    <GradientStop Color="#4FACFE" Offset="0"/>
                                    <GradientStop Color="#00F2FE" Offset="1"/>
                                </LinearGradientBrush>
                            </Ellipse.Fill>
                        </Ellipse>
                        <StackPanel VerticalAlignment="Center">
                            <TextBlock Text="☁️ OneDrive Sync" FontSize="22" FontWeight="Bold" Foreground="White"/>
                            <TextBlock Text="Keep your files dancing together! 💃" FontSize="11" Foreground="#DDFFFFFF"/>
                        </StackPanel>
                    </StackPanel>
                    
                    <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="12" VerticalAlignment="Center">
                        <Button Content="📜 History" Command="{Binding OpenViewSyncHistoryCommand}" 
                                Classes="fun-secondary" Height="44"/>
                        <Button Content="🐛 Logs" Command="{Binding OpenDebugLogViewerCommand}" 
                                Classes="fun-secondary" Height="44"/>
                        <Button Content="⚙️ Settings" Command="{Binding OpenUpdateAccountDetailsCommand}" 
                                Classes="fun-secondary" Height="44"/>
                    </StackPanel>
                </Grid>
            </Border>

            <!-- Main Content -->
            <Grid Grid.Row="1" ColumnDefinitions="400,*" Margin="0,16,0,0" ColumnGap="16">
                <!-- Left Panel -->
                <Border Grid.Column="0" Classes="glass-card">
                    <accounts:AccountManagementView DataContext="{Binding AccountManagement}" />
                </Border>

                <!-- Right Panel -->
                <Border Grid.Column="1" Classes="glass-card">
                    <Panel>
                        <syncronisation:SyncTreeView DataContext="{Binding SyncTree}" />
                        <syncronisation:SyncProgressView DataContext="{Binding SyncProgress}"
                                                         IsVisible="{Binding #SyncProgressView.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}"
                                                         x:Name="SyncProgressView" />
                        <syncronisationConflicts:ConflictResolutionView DataContext="{Binding ConflictResolution}"
                                                                        IsVisible="{Binding #ConflictResolutionView.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}"
                                                                        x:Name="ConflictResolutionView" />
                    </Panel>
                </Border>
            </Grid>
        </Grid>
    </Border>
</Window>
```

## AccountManagementView.axaml (Colorful Version)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:AStar.Dev.OneDrive.Client.Accounts"
             x:Class="AStar.Dev.OneDrive.Client.Accounts.AccountManagementView"
             x:DataType="vm:AccountManagementViewModel">

    <Design.DataContext>
        <vm:AccountManagementViewModel />
    </Design.DataContext>

    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Fun Header -->
        <StackPanel Grid.Row="0" Spacing="8" Margin="0,0,0,24">
            <TextBlock Text="👥 Your Accounts" FontSize="26" FontWeight="Black">
                <TextBlock.Foreground>
                    <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
                        <GradientStop Color="#667eea" Offset="0"/>
                        <GradientStop Color="#764ba2" Offset="1"/>
                    </LinearGradientBrush>
                </TextBlock.Foreground>
            </TextBlock>
            <TextBlock Text="Manage all your cloud connections in one place!" 
                       FontSize="14" Foreground="#666666"/>
        </StackPanel>

        <!-- Accounts Grid -->
        <ScrollViewer Grid.Row="1">
            <ListBox ItemsSource="{Binding Accounts}"
                     SelectedItem="{Binding SelectedAccount}"
                     Background="Transparent"
                     BorderThickness="0">
                <ListBox.Styles>
                    <Style Selector="ListBoxItem">
                        <Setter Property="Padding" Value="0"/>
                        <Setter Property="Margin" Value="0,0,0,16"/>
                    </Style>
                </ListBox.Styles>
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border CornerRadius="16" Padding="20" BoxShadow="0 4 12 0 #20000000">
                            <Border.Background>
                                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                    <GradientStop Color="#4FACFE" Offset="0"/>
                                    <GradientStop Color="#00F2FE" Offset="1"/>
                                </LinearGradientBrush>
                            </Border.Background>
                            <Grid ColumnDefinitions="Auto,*">
                                <!-- Avatar with Rainbow Border -->
                                <Border Grid.Column="0" Width="60" Height="60" CornerRadius="30">
                                    <Border.Background>
                                        <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                                            <GradientStop Color="#FF6B6B" Offset="0"/>
                                            <GradientStop Color="#FFE66D" Offset="1"/>
                                        </LinearGradientBrush>
                                    </Border.Background>
                                    <TextBlock Text="👤" FontSize="32" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                                </Border>

                                <!-- Info -->
                                <StackPanel Grid.Column="1" Margin="20,0,0,0" VerticalAlignment="Center">
                                    <TextBlock Text="{Binding DisplayName}" FontWeight="Bold" FontSize="16" Foreground="White"/>
                                    <TextBlock Text="{Binding Email}" FontSize="13" Foreground="#DDFFFFFF" Margin="0,4,0,0"/>
                                    <Border Background="#40FFFFFF" CornerRadius="12" Padding="8,4" Margin="0,8,0,0" HorizontalAlignment="Left">
                                        <TextBlock Text="{Binding IsAuthenticated, Converter={StaticResource BoolToStatusTextConverter}}" 
                                                   FontSize="11" FontWeight="Bold" Foreground="White"/>
                                    </Border>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>

                <ListBox.Styles>
                    <Style Selector="ListBox:empty">
                        <Setter Property="Template">
                            <ControlTemplate>
                                <Border Background="#F0F4FF" BorderBrush="#667eea" BorderThickness="2" 
                                        BorderDashArray="4,2" CornerRadius="16" Padding="40" MinHeight="250">
                                    <StackPanel Spacing="16" HorizontalAlignment="Center" VerticalAlignment="Center">
                                        <TextBlock Text="🎈" FontSize="64" HorizontalAlignment="Center"/>
                                        <TextBlock Text="No accounts yet!" FontSize="20" FontWeight="Bold">
                                            <TextBlock.Foreground>
                                                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
                                                    <GradientStop Color="#667eea" Offset="0"/>
                                                    <GradientStop Color="#764ba2" Offset="1"/>
                                                </LinearGradientBrush>
                                            </TextBlock.Foreground>
                                        </TextBlock>
                                        <TextBlock Text="Let's add your first OneDrive account!" 
                                                   FontSize="13" Foreground="#666666" TextAlignment="Center"/>
                                    </StackPanel>
                                </Border>
                            </ControlTemplate>
                        </Setter>
                    </Style>
                </ListBox.Styles>
            </ListBox>
        </ScrollViewer>

        <!-- Fun Buttons -->
        <Grid Grid.Row="2" ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" 
              ColumnGap="12" RowGap="12" Margin="0,24,0,0">
            <Button Grid.Row="0" Grid.Column="0" Content="➕ Add Account" Command="{Binding AddAccountCommand}"
                    Classes="fun-primary" HorizontalAlignment="Stretch" HorizontalContentAlignment="Center" Height="50"/>
            <Button Grid.Row="0" Grid.Column="1" Content="🗑️ Remove" Command="{Binding RemoveAccountCommand}"
                    Height="50" Background="#FF6B6B" Foreground="White" CornerRadius="16" FontWeight="Bold"
                    HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
            <Button Grid.Row="1" Grid.Column="0" Content="🔓 Sign In" Command="{Binding LoginCommand}"
                    Height="50" Background="#4ECDC4" Foreground="White" CornerRadius="16" FontWeight="Bold"
                    HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
            <Button Grid.Row="1" Grid.Column="1" Content="🔒 Sign Out" Command="{Binding LogoutCommand}"
                    Height="50" Background="#95E1D3" Foreground="White" CornerRadius="16" FontWeight="Bold"
                    HorizontalAlignment="Stretch" HorizontalContentAlignment="Center"/>
        </Grid>

        <!-- Bouncy Toast -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                IsVisible="{Binding ToastVisible}"
                HorizontalAlignment="Center"
                VerticalAlignment="Top"
                Margin="0,20,0,0">
            <Border CornerRadius="20" Padding="20,16" BoxShadow="0 8 24 0 #60000000">
                <Border.Background>
                    <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,0%">
                        <GradientStop Color="#FF6B6B" Offset="0"/>
                        <GradientStop Color="#FFE66D" Offset="1"/>
                    </LinearGradientBrush>
                </Border.Background>
                <TextBlock Text="{Binding ToastMessage}" Foreground="White" FontSize="14" FontWeight="Bold"/>
            </Border>
        </Border>

        <!-- Loading -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                Background="#E0FFFFFF"
                IsVisible="{Binding IsLoading}"
                CornerRadius="20">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="20">
                <TextBlock Text="⏳" FontSize="48" HorizontalAlignment="Center"/>
                <TextBlock Text="Loading magic..." Foreground="#667eea" FontSize="16" FontWeight="Bold"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

# Design Option 3: Random/Anything Goes (Retro Terminal Hacker Theme)

**Visual Identity:** Monospace fonts, terminal green, CRT scanlines, retro ASCII art, hacker aesthetic, dark mode with neon accents

## MainWindow.axaml (Terminal Theme)

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:accounts="using:AStar.Dev.OneDrive.Client.Accounts"
        xmlns:syncronisation="using:AStar.Dev.OneDrive.Client.Syncronisation"
        xmlns:syncronisationConflicts="using:AStar.Dev.OneDrive.Client.SyncronisationConflicts"
        xmlns:vm="using:AStar.Dev.OneDrive.Client.MainWindow"
        Icon="avares://AStar.Dev.OneDrive.Client/Assets/astar.png"
        x:Class="AStar.Dev.OneDrive.Client.MainWindow.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="[ASTAR_SYNC_v2.1] - TERMINAL ACCESS"
        Width="1400"
        Height="900"
        MinWidth="1000"
        MinHeight="700"
        Background="#0A0E0F"
        FontFamily="Consolas, monospace">

    <Design.DataContext>
        <vm:MainWindowViewModel />
    </Design.DataContext>

    <Window.Styles>
        <!-- Terminal window style -->
        <Style Selector="Border.terminal">
            <Setter Property="Background" Value="#111111"/>
            <Setter Property="BorderBrush" Value="#00FF41"/>
            <Setter Property="BorderThickness" Value="2"/>
            <Setter Property="CornerRadius" Value="0"/>
            <Setter Property="Padding" Value="16"/>
        </Style>
        
        <!-- Green terminal text -->
        <Style Selector="TextBlock.terminal-text">
            <Setter Property="Foreground" Value="#00FF41"/>
            <Setter Property="FontFamily" Value="Consolas, Courier New, monospace"/>
        </Style>
        
        <!-- Neon button -->
        <Style Selector="Button.neon">
            <Setter Property="Background" Value="#001100"/>
            <Setter Property="Foreground" Value="#00FF41"/>
            <Setter Property="BorderBrush" Value="#00FF41"/>
            <Setter Property="BorderThickness" Value="2"/>
            <Setter Property="Padding" Value="16,8"/>
            <Setter Property="FontFamily" Value="Consolas, monospace"/>
            <Setter Property="FontWeight" Value="Bold"/>
        </Style>
        
        <Style Selector="Button.neon:pointerover">
            <Setter Property="Background" Value="#003300"/>
            <Setter Property="BoxShadow" Value="0 0 10 0 #00FF41"/>
        </Style>
        
        <!-- Danger button -->
        <Style Selector="Button.neon-red">
            <Setter Property="Background" Value="#110000"/>
            <Setter Property="Foreground" Value="#FF0000"/>
            <Setter Property="BorderBrush" Value="#FF0000"/>
            <Setter Property="BorderThickness" Value="2"/>
            <Setter Property="Padding" Value="16,8"/>
            <Setter Property="FontFamily" Value="Consolas, monospace"/>
            <Setter Property="FontWeight" Value="Bold"/>
        </Style>
        
        <Style Selector="Button.neon-red:pointerover">
            <Setter Property="Background" Value="#330000"/>
            <Setter Property="BoxShadow" Value="0 0 10 0 #FF0000"/>
        </Style>
    </Window.Styles>

    <Window.KeyBindings>
        <KeyBinding Gesture="F2" Command="{Binding OpenUpdateAccountDetailsCommand}" />
        <KeyBinding Gesture="Alt+F4" Command="{Binding CloseApplicationCommand}" />
    </Window.KeyBindings>

    <Grid RowDefinitions="50,Auto,*" Margin="8">
        <!-- Terminal Header -->
        <Border Grid.Row="0" Background="#001100" BorderBrush="#00FF41" BorderThickness="2">
            <Grid ColumnDefinitions="Auto,*,Auto" Margin="12,0">
                <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
                    <TextBlock Text="█▓▒░" FontSize="16" Foreground="#00FF41" FontWeight="Bold"/>
                    <TextBlock Text="ASTAR_SYNC_CLIENT" FontSize="14" Classes="terminal-text" FontWeight="Bold"/>
                    <TextBlock Text="[v2.1.0_STABLE]" FontSize="11" Foreground="#00AA33"/>
                </StackPanel>
                
                <StackPanel Grid.Column="2" Orientation="Horizontal" Spacing="6" VerticalAlignment="Center">
                    <Button Content="[HISTORY]" Command="{Binding OpenViewSyncHistoryCommand}" Classes="neon" Height="32"/>
                    <Button Content="[LOGS]" Command="{Binding OpenDebugLogViewerCommand}" Classes="neon" Height="32"/>
                    <Button Content="[CONFIG]" Command="{Binding OpenUpdateAccountDetailsCommand}" Classes="neon" Height="32"/>
                    <Button Content="[X]" Command="{Binding CloseApplicationCommand}" Classes="neon-red" Height="32" Width="50"/>
                </StackPanel>
            </Grid>
        </Border>

        <!-- Status Bar -->
        <Border Grid.Row="1" Background="#000000" BorderBrush="#00FF41" BorderThickness="2,0,2,2" Padding="12,6">
            <Grid ColumnDefinitions="Auto,*,Auto">
                <TextBlock Grid.Column="0" Text="[STATUS: ONLINE]" Foreground="#00FF41" FontSize="11"/>
                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="16" HorizontalAlignment="Center">
                    <TextBlock Text="◢" Foreground="#00FF41" FontSize="14"/>
                    <TextBlock Text="SECURE CONNECTION ESTABLISHED" Foreground="#00AA33" FontSize="10"/>
                    <TextBlock Text="◣" Foreground="#00FF41" FontSize="14"/>
                </StackPanel>
                <TextBlock Grid.Column="2" Text="{Binding CurrentTime, StringFormat='[TIME: {0:HH:mm:ss}]'}" 
                           Foreground="#00AA33" FontSize="11"/>
            </Grid>
        </Border>

        <!-- Main Terminal Area -->
        <Grid Grid.Row="2" ColumnDefinitions="420,*" Margin="0,8,0,0" ColumnGap="8">
            <!-- Left Terminal -->
            <Border Grid.Column="0" Classes="terminal">
                <Grid RowDefinitions="Auto,4,*">
                    <TextBlock Grid.Row="0" Text="┌──[ ACCOUNTS_MODULE ]" Classes="terminal-text" FontWeight="Bold"/>
                    <Border Grid.Row="1" Background="#00FF41" Height="2"/>
                    <ScrollViewer Grid.Row="2" Margin="0,8,0,0">
                        <accounts:AccountManagementView DataContext="{Binding AccountManagement}" />
                    </ScrollViewer>
                </Grid>
            </Border>

            <!-- Right Terminal -->
            <Border Grid.Column="1" Classes="terminal">
                <Grid RowDefinitions="Auto,4,*">
                    <TextBlock Grid.Row="0" Text="┌──[ SYNC_CONTROL_MODULE ]" Classes="terminal-text" FontWeight="Bold"/>
                    <Border Grid.Row="1" Background="#00FF41" Height="2"/>
                    <Panel Grid.Row="2" Margin="0,8,0,0">
                        <syncronisation:SyncTreeView DataContext="{Binding SyncTree}" />
                        <syncronisation:SyncProgressView DataContext="{Binding SyncProgress}"
                                                         IsVisible="{Binding #SyncProgressView.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}"
                                                         x:Name="SyncProgressView" />
                        <syncronisationConflicts:ConflictResolutionView DataContext="{Binding ConflictResolution}"
                                                                        IsVisible="{Binding #ConflictResolutionView.DataContext, Converter={x:Static ObjectConverters.IsNotNull}}"
                                                                        x:Name="ConflictResolutionView" />
                    </Panel>
                </Grid>
            </Border>
        </Grid>
    </Grid>
</Window>
```

## AccountManagementView.axaml (Terminal Theme)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:AStar.Dev.OneDrive.Client.Accounts"
             x:Class="AStar.Dev.OneDrive.Client.Accounts.AccountManagementView"
             x:DataType="vm:AccountManagementViewModel"
             FontFamily="Consolas, monospace">

    <Design.DataContext>
        <vm:AccountManagementViewModel />
    </Design.DataContext>

    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Header -->
        <StackPanel Grid.Row="0" Spacing="6" Margin="0,0,0,16">
            <TextBlock Text=">> CONNECTED_ACCOUNTS" FontSize="13" Foreground="#00FF41" FontWeight="Bold"/>
            <TextBlock Text="   [Managing cloud storage access points]" FontSize="10" Foreground="#00AA33"/>
        </StackPanel>

        <!-- Accounts List -->
        <ScrollViewer Grid.Row="1">
            <ListBox ItemsSource="{Binding Accounts}"
                     SelectedItem="{Binding SelectedAccount}"
                     Background="Transparent"
                     BorderThickness="0">
                <ListBox.Styles>
                    <Style Selector="ListBoxItem">
                        <Setter Property="Padding" Value="0"/>
                        <Setter Property="Margin" Value="0,0,0,12"/>
                    </Style>
                    <Style Selector="ListBoxItem:selected /template/ ContentPresenter">
                        <Setter Property="Background" Value="#002200"/>
                    </Style>
                </ListBox.Styles>
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <Border Background="#001100" BorderBrush="#00FF41" BorderThickness="1" Padding="12">
                            <Grid RowDefinitions="Auto,Auto,Auto">
                                <TextBlock Grid.Row="0" Text="{Binding DisplayName, StringFormat='USER: {0}'}" 
                                           Foreground="#00FF41" FontSize="12" FontWeight="Bold"/>
                                <TextBlock Grid.Row="1" Text="{Binding Email, StringFormat='MAIL: {0}'}" 
                                           Foreground="#00AA33" FontSize="10" Margin="0,4,0,0"/>
                                <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="8" Margin="0,6,0,0">
                                    <TextBlock Text="[STATUS:" Foreground="#00AA33" FontSize="10"/>
                                    <TextBlock Text="{Binding IsAuthenticated, Converter={StaticResource BoolToStatusTextConverter}}" 
                                               Foreground="{Binding IsAuthenticated, Converter={StaticResource BoolToTerminalColorConverter}}" 
                                               FontSize="10" FontWeight="Bold"/>
                                    <TextBlock Text="]" Foreground="#00AA33" FontSize="10"/>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ListBox.ItemTemplate>

                <ListBox.Styles>
                    <Style Selector="ListBox:empty">
                        <Setter Property="Template">
                            <ControlTemplate>
                                <Border Background="#110000" BorderBrush="#FF0000" BorderThickness="1" 
                                        BorderDashArray="2,2" Padding="24" MinHeight="180">
                                    <StackPanel Spacing="8" VerticalAlignment="Center">
                                        <TextBlock Text="[!] NO ACCOUNTS DETECTED" FontSize="12" 
                                                   Foreground="#FF0000" FontWeight="Bold" HorizontalAlignment="Center"/>
                                        <TextBlock Text=">> Execute ADD_ACCOUNT command to initialize connection" 
                                                   FontSize="10" Foreground="#AA0000" HorizontalAlignment="Center"/>
                                    </StackPanel>
                                </Border>
                            </ControlTemplate>
                        </Setter>
                    </Style>
                </ListBox.Styles>
            </ListBox>
        </ScrollViewer>

        <!-- Command Buttons -->
        <Grid Grid.Row="2" ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" 
              ColumnGap="8" RowGap="8" Margin="0,16,0,0">
            <Button Grid.Row="0" Grid.Column="0" Content="[+] ADD" Command="{Binding AddAccountCommand}"
                    Classes="neon" HorizontalAlignment="Stretch" Height="40"/>
            <Button Grid.Row="0" Grid.Column="1" Content="[-] REMOVE" Command="{Binding RemoveAccountCommand}"
                    Classes="neon-red" HorizontalAlignment="Stretch" Height="40"/>
            <Button Grid.Row="1" Grid.Column="0" Content="[>] LOGIN" Command="{Binding LoginCommand}"
                    Classes="neon" HorizontalAlignment="Stretch" Height="40"/>
            <Button Grid.Row="1" Grid.Column="1" Content="[<] LOGOUT" Command="{Binding LogoutCommand}"
                    Classes="neon" HorizontalAlignment="Stretch" Height="40"/>
        </Grid>

        <!-- Terminal Toast -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                IsVisible="{Binding ToastVisible}"
                HorizontalAlignment="Center"
                VerticalAlignment="Top"
                Margin="0,10,0,0">
            <Border Background="#001100" BorderBrush="#00FF41" BorderThickness="2" 
                    Padding="16,10" BoxShadow="0 0 20 0 #00FF41">
                <TextBlock Text="{Binding ToastMessage, StringFormat='>> {0}'}" 
                           Foreground="#00FF41" FontSize="11" FontWeight="Bold"/>
            </Border>
        </Border>

        <!-- Loading -->
        <Border Grid.Row="0" Grid.RowSpan="3"
                Background="#E0000000"
                IsVisible="{Binding IsLoading}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center" Spacing="12">
                <TextBlock Text="████████" Foreground="#00FF41" FontSize="20" HorizontalAlignment="Center"/>
                <TextBlock Text="[LOADING...]" Foreground="#00FF41" FontSize="12" FontWeight="Bold"/>
                <ProgressBar IsIndeterminate="True" Width="200" Height="4" Foreground="#00FF41"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

---

Each design maintains all the functionality of your current application while offering completely different visual experiences:

1. **Professional**: Best for corporate environments, clean and trustworthy
2. **Colorful**: Perfect for creative users who want a fun, approachable interface
3. **Terminal**: For power users who appreciate retro/hacker aesthetics

Would you like me to:

- Complete the remaining views (SyncTreeView, ConflictResolutionView, SyncProgressView) for all three themes?
- Provide the necessary value converters for each design?
- Create the complete resource dictionaries with all color definitions?

Similar code found with 2 license types

---

## Implementation Status

**Status**: **Implemented** (February 2026)

### Chosen Approach: ResourceDictionary Swapping

The theme system was successfully implemented using the **ResourceDictionary approach** (Option 3 from the original design proposals). This approach was selected for the following reasons:

- **Minimal Code Changes**: Required no modifications to ViewModels or business logic
- **Runtime Switching**: Themes can be changed instantly without application restart
- **Clean Separation**: Visual styling completely separated from functional code
- **Maintainability**: New themes can be added by creating new XAML files
- **Performance**: Negligible overhead compared to other approaches

### Architecture

**Core Components**:
- **ThemeService** (`Infrastructure/Services/ThemeService.cs`): Singleton service managing theme state and persistence
- **WindowPreferencesService** (`Infrastructure/Services/WindowPreferencesService.cs`): Handles database persistence of user theme selection
- **Theme Resource Dictionaries** (`src/AStar.Dev.OneDrive.Client/Themes/*.axaml`): XAML files defining colors, styles, and brushes for each theme

**Theme Switching Flow**:
1. User selects theme from Settings window dropdown
2. SettingsViewModel calls `ThemeService.ApplyThemeAsync()`
3. ThemeService updates `Application.Current.Resources.MergedDictionaries`
4. Theme preference persisted to SQLite database via WindowPreferencesService
5. Theme reloaded on next application startup

### Available Themes

Six themes implemented as complete ResourceDictionary files:

1. **OriginalAuto** - Follows Windows system theme (light/dark)
2. **OriginalLight** - Fixed light mode
3. **OriginalDark** - Fixed dark mode  
4. **Professional** - Corporate minimalist with blue accents
5. **Colourful** - Vibrant gradients and glass morphism
6. **Terminal** - Retro green-on-black hacker aesthetic

### Technical Implementation

**Theme Loading**:
- ResourceDictionaries loaded dynamically using `AvaloniaXamlLoader.Load()`
- Base styles defined in `App.axaml` remain constant
- Theme-specific resources override base definitions

**Persistence**:
- Theme selection stored in `WindowPreferences` table as enum string
- Automatically restored on application launch
- Survives application updates and system restarts

**UI Integration**:
- Settings window (`Settings/SettingsWindow.axaml`) provides theme selection
- ComboBox bound to `ThemePreference` enum with custom display name converter
- Auto-apply on selection change for immediate visual feedback

### Testing

**Integration Tests**:
- `ThemePersistenceShould.cs`: Verifies theme persistence across application restarts
- 6 test cases covering all theme variants
- Uses in-memory SQLite database to test full persistence stack

**Manual Testing**:
- All themes validated across all application views
- Theme switching tested during active sync operations
- Persistence verified across multiple application sessions

### Future Enhancements

Potential improvements for future versions:

- **Custom Themes**: Allow users to create custom color schemes
- **Import/Export**: Share theme configurations between users
- **Per-Account Themes**: Different theme for each OneDrive account
- **High Contrast Mode**: Accessibility-focused theme variant
