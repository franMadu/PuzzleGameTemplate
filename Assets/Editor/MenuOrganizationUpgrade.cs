using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// All layout, icons and labels are authored in the scene, including migrations of an open scene.
public static class MenuOrganizationUpgrade
{
    private static readonly Color Surface = new Color(.065f,.09f,.12f,.96f);
    private static readonly Color Gold = new Color(.85f,.69f,.40f,1);
    private static Font font;
    private static Button template;

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += RepairOpenMenus;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += RepairOpenMenus;
    }
    private static void RepairOpenMenus()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var menu in Resources.FindObjectsOfTypeAll<PuzzleMenu>())
        {
            if (!menu.gameObject.scene.IsValid() || !menu.gameObject.scene.isLoaded
                || EditorSceneManager.IsPreviewScene(menu.gameObject.scene)
                || menu.gameObject.scene.path != "Assets/Scenes/MainMenu.unity") continue;
            if (new SerializedObject(menu).FindProperty("menuLayoutVersion").intValue >= 1) continue;
            StarEconomyCanvasUpgrade.RepairLoadedMenus();
            if (new SerializedObject(menu).FindProperty("shopScreen").objectReferenceValue != null) Apply(menu);
        }
    }

    [MenuItem("Tools/One Hand Puzzle/Organize Menus")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var menu = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PuzzleMenu>(true)).Single();
        Apply(menu);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("MENU_ORGANIZATION_UPGRADE_PASS");
    }
    public static void Apply(PuzzleMenu menu)
    {
        var data = new SerializedObject(menu);
        RectTransform Screen(string field) => (RectTransform)data.FindProperty(field).objectReferenceValue;
        Transform Content(string field) => Screen(field).Find("Content");
        template = Content("mainScreen").Find("Recap").GetComponent<Button>();
        font = template.GetComponentInChildren<Text>(true).font;
        if (data.FindProperty("challengesScreen").objectReferenceValue == null)
        {
            var copy = Object.Instantiate(Screen("shopScreen").gameObject, Screen("shopScreen").parent);
            copy.name = "Challenges";
            var content = copy.transform.Find("Content");
            Object.DestroyImmediate(content.GetComponent<StarShop>());
            content.gameObject.AddComponent<PuzzleChallenges>();
            foreach(string name in new[]{"Wallpapers","Puzzles"}) Object.DestroyImmediate(content.Find(name).gameObject);
            data.FindProperty("challengesScreen").objectReferenceValue = copy.GetComponent<RectTransform>();
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (string field in new[]{"mainScreen","galleryScreen","selectionScreen","settingsScreen","chaptersScreen","subchaptersScreen","difficultyScreen","shopScreen","challengesScreen"})
        {
            var screen = Screen(field);
            var content = screen.Find("Content");
            StretchContent(content);
            if (field != "mainScreen") screen.GetComponent<Image>().color = Surface;
            Header(content, field == "mainScreen");
            if(field=="settingsScreen")content.Find("Options").gameObject.SetActive(false);
            Navigation(content);
            if (content.Find("Pages") != null) Place(content.Find("Pages"),.35f,.115f,.65f,.17f);
            if (content.Find("Empty") != null) Place(content.Find("Empty"),.20f,.35f,.90f,.65f);
        }
        Main(Content("mainScreen"));
        Gallery(Content("galleryScreen"));
        Cards(Content("shopScreen"), true);
        Cards(Content("challengesScreen"), false);
        Caption(Content("shopScreen").Find("Title"), "Tienda", 30);
        Caption(Content("challengesScreen").Find("Title"), "Desafíos", 30);
        Caption(Content("chaptersScreen").Find("Title"), "Archivo de capítulos", 30);
        ChapterRows(Content("chaptersScreen"), true);
        ChapterRows(Content("subchaptersScreen"), false);
        Details(Content("difficultyScreen"), false);
        Details(Content("selectionScreen"), true);
        Settings(Content("settingsScreen"));
        Viewer(Screen("imageScreen"));
        var title = Content("titleScreen");
        StretchContent(title);
        Place(title.Find("Title"),.10f,.48f,.90f,.73f);
        title.Find("Title").GetComponent<Text>().color = Gold;
        Place(title.Find("Start"),.27f,.22f,.73f,.31f);
        StyleButton(title.Find("Start"), true);
        data.Update();
        data.FindProperty("menuLayoutVersion").intValue = 1;
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
    }

    private static void Header(Transform content, bool main)
    {
        var bar = Node(content,"Header",true);
        Place(bar,0,.89f,1,1); bar.SetAsFirstSibling();
        bar.GetComponent<Image>().color = new Color(.035f,.055f,.075f, main ? .78f : 1);
        if (!main)
        {
            var back = content.Find("Back");
            Place(back,0,.90f,.12f,.99f); StyleButton(back,false); Caption(back.Find("Label"),"Volver",20);
            var title = content.Find("Title");
            Place(title,.145f,.90f,.60f,.99f);
            var text=title.GetComponent<Text>(); text.fontSize=30; text.alignment=TextAnchor.MiddleLeft; text.color=PuzzleUI.Ink;
        }
        var wallet = content.Find("Wallet");
        if (wallet == null)
        {
            wallet = Node(content,"Wallet",true);
            var icon = Node(wallet,"Icon").gameObject.AddComponent<PuzzleStarsGraphic>();
            icon.Slots=1; icon.Stars=1; icon.color=Gold; icon.raycastTarget=false;
            Label(wallet,"Amount","0",28);
        }
        Place(wallet,.65f,.915f,.85f,.98f);
        wallet.GetComponent<Image>().color=PuzzleUI.Panel;
        Place(wallet.Find("Icon"),.04f,.20f,.29f,.80f);
        Place(wallet.Find("Amount"),.35f,.10f,.97f,.90f);
        var options=content.Find(main ? "Settings" : "Options");
        if(options==null)options=Button(content,"Options","Opciones");
        Place(options,.87f,.915f,1,.98f); StyleButton(options,false);
        var label=options.Find("Label");
        if(label!=null)Caption(label,"Opciones",18);
    }
    private static void Navigation(Transform content)
    {
        var nav=Node(content,"Navigation",true); Place(nav,0,0,1,.10f);
        nav.GetComponent<Image>().color=new Color(.035f,.055f,.075f,.97f);
        string[] names={"Home","Chapters","Gallery","Challenges","Shop"};
        string[] captions={"Inicio","Capítulos","Galería","Desafíos","Tienda"};
        for(int i=0;i<names.Length;i++)
        {
            var button=content.Find("Navigation/"+names[i])??Button(nav,names[i],captions[i]);
            Place(button,i/5f,0,(i+1)/5f,1,6);
            StyleButton(button,false);
            var icon=Node(button,"Icon"); Place(icon,.35f,.43f,.65f,.93f);
            if(icon.GetComponent<CanvasRenderer>()==null)icon.gameObject.AddComponent<CanvasRenderer>();
            var graphic=icon.GetComponent<MenuIconGraphic>()??icon.gameObject.AddComponent<MenuIconGraphic>();
            graphic.Icon=(MenuIcon)i; graphic.color=Gold; graphic.raycastTarget=false;
            Place(button.Find("Label"),0,.02f,1,.39f); Caption(button.Find("Label"),captions[i],19);
        }
    }
    private static void Main(Transform content)
    {
        foreach(string name in new[]{"Recap","Gallery","Shop"})content.Find(name).gameObject.SetActive(false);
        var nameLabel=Label(content,"Game Title","ONE HAND PUZZLE",30);
        Place(nameLabel.transform,.02f,.90f,.58f,.99f); nameLabel.alignment=TextAnchor.MiddleLeft; nameLabel.color=Gold;
        var panel=Node(content,"Story Card",true); Place(panel,.56f,.38f,.98f,.64f,8);
        panel.GetComponent<Image>().color=new Color(.045f,.075f,.10f,.91f);
        Label(panel,"Category","HISTORIA PRINCIPAL",18).color=Gold;
        Place(panel.Find("Category"),.06f,.73f,.94f,.96f);
        Label(panel,"Location","Tu siguiente escena",30).alignment=TextAnchor.MiddleLeft;
        Place(panel.Find("Location"),.06f,.26f,.94f,.72f);
        var state=Label(panel,"State","Continúa tu viaje",18); state.alignment=TextAnchor.MiddleLeft;
        Formats(state,"Continúa tu viaje","Historia completada · Volver a empezar");
        Place(state.transform,.06f,.02f,.94f,.23f);
        var story=content.Find("Story"); Place(story,.56f,.21f,.98f,.37f,8); StyleButton(story,true);
        Caption(story.Find("Label"),"Continuar historia",30);
    }
    private static void Gallery(Transform content)
    {
        Caption(content.Find("Title"),"Galería",30);
        Place(content.Find("Puzzles"),.05f,.81f,.48f,.87f); StyleButton(content.Find("Puzzles"),false);
        Place(content.Find("Wallpapers"),.52f,.81f,.95f,.87f); StyleButton(content.Find("Wallpapers"),false);
        for(int i=0;i<6;i++)
        {
            var card=content.Find("Image "+i);
            Grid(card,i,0.04f,.96f,.20f,.78f,3);
            StyleButton(card,false);
            var art=card.Find("Artwork"); Place(art,.025f,.025f,.975f,.975f);
            FitArtwork(art);
        }
    }
    private static void Cards(Transform content, bool store)
    {
        if(store)
        {
            var side=Node(content,"Categories",true); Place(side,.02f,.20f,.205f,.85f);
            side.GetComponent<Image>().color=new Color(.045f,.065f,.09f,1); side.SetAsFirstSibling();
            Place(content.Find("Wallpapers"),.025f,.65f,.20f,.83f,4); StyleButton(content.Find("Wallpapers"),false);
            Place(content.Find("Puzzles"),.025f,.45f,.20f,.63f,4); StyleButton(content.Find("Puzzles"),false);
        }
        else Caption(content.Find("Empty"),"Los desafíos aparecerán aquí al avanzar en la historia o desbloquearlos en la tienda.",24);
        for(int i=0;i<6;i++)
        {
            var card=content.Find("Row "+i);
            Grid(card,i,store?.23f:.04f,.97f,.20f,.85f,3);
            card.GetComponent<Image>().color=PuzzleUI.Panel;
            Place(card.Find("Artwork"),.035f,.46f,.965f,.965f); FitArtwork(card.Find("Artwork"));
            Place(card.Find("Title"),.06f,.29f,.94f,.44f); card.Find("Title").GetComponent<Text>().fontSize=22;
            Place(card.Find("Price"),.06f,.17f,store?.94f:.53f,.28f); card.Find("Price").GetComponent<Text>().fontSize=20;
            Place(card.Find("Buy"),.05f,.025f,.95f,.16f); StyleButton(card.Find("Buy"),true);
            if(!store)
            {
                Formats(card.Find("Price").GetComponent<Text>(),"Nuevo","Récord","En tienda","Historia");
                Formats(card.Find("Buy/Label").GetComponent<Text>(),"Jugar","Ir a tienda","Bloqueado");
                var stars=Node(card,"Stars"); Place(stars,.56f,.17f,.95f,.28f);
                var graphic=stars.GetComponent<PuzzleStarsGraphic>()??stars.gameObject.AddComponent<PuzzleStarsGraphic>();
                graphic.color=Gold; graphic.raycastTarget=false;
            }
        }
    }
    private static void ChapterRows(Transform content,bool chapters)
    {
        for(int i=0;i<6;i++)
        {
            var row=content.Find("Row "+i);
            if(chapters)Grid(row,i,.04f,.96f,.20f,.85f,2);
            else Place(row,.04f,.20f+(5-i)*.108f,.96f,.20f+(5-i)*.108f+.098f,2);
            StyleButton(row,false);
            var art=Node(row,"Artwork").GetComponent<RawImage>()??row.Find("Artwork").gameObject.AddComponent<RawImage>();
            Place(art.transform,.025f,.08f,chapters?.37f:.19f,.92f); art.raycastTarget=false; FitArtwork(art.transform);
            float left=chapters?.40f:.22f;
            Place(row.Find("Label"),left,.43f,chapters?.95f:.70f,.90f); var label=row.Find("Label").GetComponent<Text>();
            label.fontSize=25; label.alignment=TextAnchor.MiddleLeft;
            var subtitle=Label(row,"Subtitle","Escena de historia",18); subtitle.alignment=TextAnchor.MiddleLeft; subtitle.color=PuzzleUI.Muted;
            Formats(subtitle,"{0} escenas completadas","Historia y puzle","Escena de historia","Continúa la historia para desbloquear");
            Place(subtitle.transform,left,.12f,.73f,.43f);
            var state=Label(row,"State","Disponible",17); state.alignment=TextAnchor.MiddleRight; state.color=Gold;
            Formats(state,"Completado","Disponible","Bloqueado"); Place(state.transform,.72f,.08f,.96f,.40f);
            var stars=Node(row,"Stars"); Place(stars,.72f,.42f,.96f,.68f);
            var graphic=stars.GetComponent<PuzzleStarsGraphic>()??stars.gameObject.AddComponent<PuzzleStarsGraphic>();
            graphic.color=Gold; graphic.raycastTarget=false;
        }
    }
    private static void Details(Transform content,bool story)
    {
        Place(content.Find("Artwork"),.04f,.20f,.53f,.83f); FitArtwork(content.Find("Artwork"));
        Place(content.Find("Hidden"),.04f,.20f,.53f,.83f);
        if(story)
        {
            Caption(content.Find("Title"),"Historia principal",30);
            Place(content.Find("Location"),.59f,.50f,.96f,.74f);
            content.Find("Location").GetComponent<Text>().fontSize=32;
            Place(content.Find("Continue"),.59f,.25f,.96f,.37f); StyleButton(content.Find("Continue"),true);
        }
        else
        {
            for(int i=0;i<3;i++)
            {
                var tab=content.Find("Difficulty "+i); Place(tab,.59f,.67f-i*.13f,.96f,.77f-i*.13f); StyleButton(tab,false);
            }
            Place(content.Find("Grade"),.59f,.29f,.96f,.37f);
            Place(content.Find("Grade/Stars"),.50f,.05f,1,.95f);
            content.Find("Grade").GetComponent<Text>().fontSize=21;
            Place(content.Find("Play"),.59f,.18f,.96f,.28f); StyleButton(content.Find("Play"),true);
        }
    }
    private static void Settings(Transform c)
    {
        Caption(c.Find("Title"),"Opciones",30);
        Place(c.Find("Sound"),.05f,.80f,.47f,.85f);
        string[] sliders={"Master Volume","Music Volume","Effects Volume","Dialogue Opacity"};
        for(int i=0;i<sliders.Length;i++)
        {
            var slider=c.Find(sliders[i]); float left=i==3?.54f:.05f; float bottom=i==3?.59f:.69f-i*.10f;
            Place(slider,left,bottom,left+.42f,bottom+.08f);
            Place(slider.Find("Title"),0,.52f,.82f,1); slider.Find("Title").GetComponent<Text>().fontSize=20;
            slider.Find("Title").GetComponent<Text>().alignment=TextAnchor.MiddleLeft;
            Place(slider.Find("Percent"),.82f,.52f,1,1);
            slider.Find("Percent").GetComponent<Text>().alignment=TextAnchor.MiddleRight;
            Place(slider.Find("Track"),.015f,.09f,.985f,.35f);
            Place(slider.Find("Handle Area"),.015f,.02f,.985f,.42f);
            ((RectTransform)slider.Find("Handle Area/Handle")).sizeDelta=new Vector2(20,28);
            slider.Find("Track/Fill").GetComponent<Image>().color=Gold;
        }
        Place(c.Find("Mute"),.05f,.39f,.47f,.46f);
        Place(c.Find("Dialogue"),.54f,.80f,.96f,.85f);
        Place(c.Find("Text Speed"),.54f,.70f,.96f,.77f);
        Place(c.Find("Display"),.54f,.49f,.96f,.54f);
        Place(c.Find("Fullscreen"),.54f,.40f,.96f,.47f);
        Place(c.Find("Resolution"),.61f,.31f,.89f,.37f);
        Place(c.Find("Previous Resolution"),.54f,.31f,.60f,.37f);
        Place(c.Find("Next Resolution"),.90f,.31f,.96f,.37f);
        Place(c.Find("God Mode"),.05f,.23f,.47f,.30f);
        Place(c.Find("Restore Defaults"),.54f,.20f,.96f,.27f);
        foreach(var button in c.GetComponentsInChildren<Button>(true))if(button.transform.parent==c)StyleButton(button.transform,false);
        foreach(string section in new[]{"Sound","Dialogue","Display"})
        {
            var label=c.Find(section).GetComponent<Text>(); label.alignment=TextAnchor.MiddleLeft; label.color=Gold;
        }
    }
    private static void Viewer(RectTransform screen)
    {
        var c=screen.Find("Controls");
        StretchContent(c);
        Place(c.Find("Back"),.015f,.91f,.14f,.985f); StyleButton(c.Find("Back"),false);
        Place(c.Find("Zoom Out"),.80f,.91f,.88f,.985f); StyleButton(c.Find("Zoom Out"),false);
        Place(c.Find("Zoom In"),.90f,.91f,.98f,.985f); StyleButton(c.Find("Zoom In"),false);
        Place(c.Find("Use Background"),.28f,.025f,.72f,.105f); StyleButton(c.Find("Use Background"),true);
    }
    private static RectTransform Node(Transform parent,string name,bool image=false)
    {
        var node=parent.Find(name) as RectTransform;
        if(node==null)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.layer=parent.gameObject.layer; go.transform.SetParent(parent,false); node=(RectTransform)go.transform;
        }
        if(image && node.GetComponent<Image>()==null)node.gameObject.AddComponent<Image>();
        if(image)node.GetComponent<Image>().raycastTarget=false;
        return node;
    }
    private static void StretchContent(Transform content)
    {
        var rect=(RectTransform)content;
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
        rect.offsetMin=new Vector2(24,24); rect.offsetMax=new Vector2(-24,-24);
    }
    private static RectTransform Button(Transform parent,string name,string caption)
    {
        var copy=Object.Instantiate(template.gameObject,parent); copy.name=name; copy.SetActive(true);
        copy.GetComponent<Button>().onClick=new Button.ButtonClickedEvent();
        Caption(copy.transform.Find("Label"),caption,22);
        return (RectTransform)copy.transform;
    }
    private static Text Label(Transform parent,string name,string caption,int size)
    {
        var rect=Node(parent,name); var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();
        text.font=font; text.text=caption; text.fontSize=size; text.color=PuzzleUI.Ink;
        text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
        return text;
    }
    private static void Caption(Transform owner,string caption,int size)
    {
        var text=owner.GetComponent<Text>();
        if(text.GetComponent<CanvasText>()!=null)Object.DestroyImmediate(text.GetComponent<CanvasText>());
        text.text=caption; text.fontSize=size; text.resizeTextForBestFit=false; text.color=PuzzleUI.Ink;
    }
    private static void StyleButton(Transform owner,bool primary)
    {
        var image=owner.GetComponent<Image>(); image.color=primary?PuzzleUI.Accent:PuzzleUI.Panel; image.raycastTarget=true;
        var button=owner.GetComponent<Button>();
        var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.15f,1.15f,1.15f,1);
        colors.pressedColor=new Color(.8f,.8f,.8f,1); colors.disabledColor=new Color(.5f,.5f,.5f,.65f); button.colors=colors;
        button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
    }
    private static void Place(Transform owner,float x1,float y1,float x2,float y2,float inset=0)
    {
        var rect=(RectTransform)owner; rect.anchorMin=new Vector2(x1,y1); rect.anchorMax=new Vector2(x2,y2);
        rect.offsetMin=Vector2.one*inset; rect.offsetMax=-Vector2.one*inset; rect.pivot=Vector2.one/2; rect.localScale=Vector3.one;
    }
    private static void Grid(Transform owner,int index,float x1,float x2,float y1,float y2,int columns)
    {
        int rows=6/columns; float w=(x2-x1)/columns,h=(y2-y1)/rows;
        Place(owner,x1+(index%columns)*w,y2-(index/columns+1)*h,x1+(index%columns+1)*w,y2-index/columns*h,7);
    }
    private static void FitArtwork(Transform owner)
    {
        // The named Artwork object remains the authored image slot; fit a child within it.
        var original=owner.GetComponent<RawImage>();
        var child=owner.Find("Image") as RectTransform;
        if(child==null)
        {
            child=Node(owner,"Image"); Place(child,0,0,1,1);
            var raw=child.gameObject.AddComponent<RawImage>();
            if(original!=null){raw.texture=original.texture; raw.color=original.color;}
        }
        if(original!=null)Object.DestroyImmediate(original);
        if(owner.GetComponent<AspectRatioFitter>()!=null)Object.DestroyImmediate(owner.GetComponent<AspectRatioFitter>());
        var fitter=child.GetComponent<AspectRatioFitter>()??child.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
        var image=child.GetComponent<RawImage>(); fitter.aspectRatio=image.texture!=null?image.texture.width/(float)image.texture.height:16f/9;
        image.raycastTarget=false;
    }
    private static void Formats(Text text,params string[] formats)
    {
        var source=text.GetComponent<CanvasText>()??text.gameObject.AddComponent<CanvasText>();
        var data=new SerializedObject(source); var values=data.FindProperty("formats"); values.arraySize=formats.Length;
        for(int i=0;i<formats.Length;i++)values.GetArrayElementAtIndex(i).stringValue=formats[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
